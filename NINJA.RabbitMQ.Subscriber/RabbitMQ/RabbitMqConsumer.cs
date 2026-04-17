using System.Buffers;
using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Stream.Client;
using RabbitMQ.Stream.Client.Reliable;
using NINJA.RabbitMQ.Subscriber.RabbitMQ.Connection;
using NINJA.RabbitMQ.Subscriber.RabbitMQ.Strategies;

namespace NINJA.RabbitMQ.Subscriber.RabbitMQ
{
    public class RabbitMqConsumer: IMessageConsumer, IAsyncDisposable
    {
        private readonly IRabbitMqConnection _connection;
        private readonly IStreamOffsetStrategyFactory _strategyFactory;
        private readonly RabbitMqSettings _settings;
        private readonly object _lock = new();

        // AMQP channel — used for classic and quorum queues
        private IModel? _channel;
        private string? _queueName;

        // Stream Client state — used exclusively for streams (port 5552, binary protocol)
        private StreamSystem? _streamSystem;
        private Consumer? _streamConsumer;  // RabbitMQ.Stream.Client.Reliable.Consumer

        // Guards against double-dispose from any combination of:
        // StopConsuming() → Dispose() / DisposeAsync()
        private volatile bool _disposed;

        public RabbitMqConsumer(
            IRabbitMqConnection connection,
            IStreamOffsetStrategyFactory strategyFactory,
            IOptions<RabbitMqSettings> settings)
        {
            _connection = connection;
            _strategyFactory = strategyFactory;
            _settings = settings.Value;
        }

        // -------------------------------------------------------------------------
        // Classic queue  (AMQP, durable, optional DLX)
        // -------------------------------------------------------------------------
        public void StartConsuming(string queueName,bool autoAck = false,Action<string>? messageHandler = null,
            string? deadLetterExchange = null)
        {
            _queueName = queueName;
            _channel = GetOrCreateChannel();

            var arguments = new Dictionary<string,object>();
            if (!string.IsNullOrEmpty(deadLetterExchange))
                arguments.Add("x-dead-letter-exchange",deadLetterExchange);

            _channel.QueueDeclare(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: arguments);

            _channel.BasicQos(0,1,false);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (_,ea) =>
            {
                var message = Encoding.UTF8.GetString(ea.Body.ToArray());
                Console.WriteLine($"[Classic] Received{(string.IsNullOrEmpty(deadLetterExchange) ? "" : $" (DLX: {deadLetterExchange})")}");
                messageHandler?.Invoke(message);
                if (!autoAck)
                    _channel.BasicAck(deliveryTag: ea.DeliveryTag,multiple: false);
                await Task.Yield();
            };

            _channel.BasicConsume(queue: queueName,autoAck: autoAck,consumer: consumer);
        }

        // -------------------------------------------------------------------------
        // Quorum queue  (AMQP, replicated, optional DLX)
        // -------------------------------------------------------------------------
        public void StartConsumingQuorum(string queueName,bool autoAck = false,Action<string>? messageHandler = null,
            string deadLetterStrategy = "at-least-once",string overflow = "reject-publish",int initialGroupSize = 0,
            string? deadLetterExchange = null)
        {
            _queueName = queueName;
            _channel = GetOrCreateChannel();

            var arguments = new Dictionary<string,object>
            {
                { "x-queue-type",             "quorum"            },
                { "x-dead-letter-strategy",   deadLetterStrategy  },
                { "x-delivery-limit",         5                   },
                { "x-overflow",               overflow            },
                { "x-single-active-consumer", true                }
            };

            if (!string.IsNullOrEmpty(deadLetterExchange))
            {
                string dlqName = $"{queueName}.dlq";
                _channel.ExchangeDeclare(exchange: deadLetterExchange,type: ExchangeType.Direct,durable: true);
                arguments.Add("x-dead-letter-exchange",deadLetterExchange);
                _channel.QueueDeclare(queue: dlqName,durable: true,exclusive: false,autoDelete: false);
                _channel.QueueBind(queue: dlqName,exchange: deadLetterExchange,routingKey: queueName);
            }

            if (initialGroupSize > 0)
                arguments.Add("x-quorum-initial-group-size",initialGroupSize);

            _channel.QueueDeclare(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: arguments);

            _channel.BasicQos(0,1,false);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (_,ea) =>
            {
                var message = Encoding.UTF8.GetString(ea.Body.ToArray());
                Console.WriteLine($"[Quorum] Received (DLX: {deadLetterExchange ?? "None"}, Strategy: {deadLetterStrategy}, Overflow: {overflow})");
                try
                {
                    messageHandler?.Invoke(message);
                    _channel.BasicAck(deliveryTag: ea.DeliveryTag,multiple: false);
                }
                catch (Exception)
                {
                    _channel.BasicNack(deliveryTag: ea.DeliveryTag,multiple: false,requeue: true);
                    if (ea.BasicProperties.Headers?.ContainsKey("x-death") == true)
                        Console.WriteLine("[Quorum] Message moved to DLQ after delivery limit reached");
                }
                await Task.Yield();
            };

            _channel.BasicConsume(queue: queueName,autoAck: autoAck,consumer: consumer);
        }

        // -------------------------------------------------------------------------
        // Stream  (RabbitMQ Stream Protocol, port 5552 — NOT AMQP)
        //
        // # Key differences from classic/quorum:
        //  - Uses a dedicated binary TCP connection on port 5552 (StreamSystem).
        //  - Non-destructive reads: messages are NEVER removed when consumed.
        //  - Every consumer chooses its own starting position (IOffsetType).
        //  - Retention is controlled by size (MaxLengthBytes) and age (MaxAge),
        //    not by consumer acknowledgement.
        //  - Acknowledgements are NOT used for message deletion — they are only
        //    used for flow-control credits inside the stream protocol.
        // -------------------------------------------------------------------------
        public async Task StartConsumingStream(
            string streamName,
            Action<string>? messageHandler = null,
            long retentionSize = 0,
            TimeSpan? retentionTime = null,
            int maxSegmentSize = 0,
            string streamOffset = "last",
            ulong? specificOffset = null)
        {
            // 1. Connect via the Stream Protocol (separate from the AMQP connection)
            _streamSystem = await StreamSystem.Create(new StreamSystemConfig
            {
                Endpoints = new List<EndPoint>
                {
                    new IPEndPoint(IPAddress.Parse(_settings.HostName == "localhost"
                        ? "127.0.0.1"
                        : _settings.HostName), _settings.StreamPort)
                },
                UserName = _settings.UserName,
                Password = _settings.Password,
                VirtualHost = _settings.VirtualHost
            });

            // 2. Declare the stream with retention policy.
            //    Idempotent: broker returns OK when the same spec is re-declared.
            var streamSpec = new StreamSpec(streamName);
            if (retentionSize > 0)
                streamSpec.MaxLengthBytes = (ulong)retentionSize;
            if (retentionTime.HasValue && retentionTime.Value > TimeSpan.Zero)
                streamSpec.MaxAge = retentionTime.Value;
            if (maxSegmentSize > 0)
                streamSpec.MaxSegmentSizeBytes = maxSegmentSize;

            await _streamSystem.CreateStream(streamSpec);

            // 3. Resolve offset strategy using Strategy pattern
            IStreamOffsetStrategy strategy = (streamOffset.ToLower() is "specific" or "offset")
                ? _strategyFactory.CreateSpecificStrategy()
                : _strategyFactory.CreateStrategy(streamOffset);

            IOffsetType offsetType = strategy.GetOffsetType(specificOffset);

            // 4. Create the consumer using the high-level Reliable Consumer
            //    (auto-reconnects on broker restart — important for a stream POC)
            _streamConsumer = await Consumer.Create(new ConsumerConfig(_streamSystem,streamName)
            {
                OffsetSpec = offsetType,
                // In RabbitMQ.Stream.Client 1.11+, MessageHandler is:
                // Func<string consumerRef, RawConsumer consumer, MessageContext ctx, Message message, Task>
                MessageHandler = async (_,_,ctx,message) =>
                {
                    var body = message.Data.Contents.ToArray();
                    var text = Encoding.UTF8.GetString(body);
                    Console.WriteLine($"[Stream] Received (offset: {ctx.Offset}, strategy: {streamOffset}, " +
                                      $"retention: {(retentionSize > 0 ? $"{retentionSize / (1024 * 1024)}MB" : "unlimited")}, " +
                                      $"max-age: {(retentionTime.HasValue ? retentionTime.Value.ToString() : "unlimited")})");
                    messageHandler?.Invoke(text);
                    await Task.CompletedTask;
                }
            });
        }

        public async Task StopConsuming()
        {
            // Delegates to DisposeAsync so the same guarded teardown path is used.
            // The consumer is meant to be discarded after stopping — there is no
            // "restart" on the same instance.
            await DisposeAsync();
        }

        // -------------------------------------------------------------------------
        // Disposal
        //
        // Only IAsyncDisposable — no sync IDisposable wrapper.
        // Callers must use:  await using var consumer = ...
        //                    await consumer.StopConsuming()
        //
        // Why no IDisposable?
        //   All teardown is genuinely async (stream protocol close, AMQP close).
        //   A sync Dispose() would block with .GetAwaiter().GetResult() which can
        //   deadlock when called from a SynchronizationContext (e.g. ASP.NET).
        //   Forcing callers to await the async path is safer and more honest.
        //
        // Why no GC.SuppressFinalize?
        //   This class has no finalizer (~RabbitMqConsumer). SuppressFinalize only
        //   matters when you have a finalizer to suppress. Without one it is a
        //   no-op — dead code copied from the full Dispose pattern boilerplate.
        // -------------------------------------------------------------------------
        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;
            _disposed = true;

            // 1. Stop the stream consumer first — it depends on the StreamSystem.
            //    Consumer.Close() returns Task<ResponseCode>; the code is discarded
            //    at teardown since there is nothing actionable to do with it.
            if (_streamConsumer is not null)
            {
                await _streamConsumer.Close();
                _streamConsumer = null;
            }

            // 2. Close the StreamSystem (binary TCP connection on port 5552).
            if (_streamSystem is not null)
            {
                await _streamSystem.Close();
                _streamSystem = null;
            }

            // 3. Close then dispose the AMQP channel (port 5672).
            //    Close() sends a clean protocol-level shutdown to the broker.
            //    Dispose() then releases the socket.
            //    Skipping Close() and going straight to Dispose() leaves the broker
            //    with a hard TCP reset instead of a graceful shutdown.
            if (_channel is not null)
            {
                if (!_channel.IsClosed)
                    _channel.Close();
                _channel.Dispose();
                _channel = null;
            }
        }

        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------
        private IModel GetOrCreateChannel()
        {
            lock (_lock)
            {
                if (_channel == null || _channel.IsClosed)
                    _channel = _connection.Connection.CreateModel();
                return _channel;
            }
        }
    }
}
