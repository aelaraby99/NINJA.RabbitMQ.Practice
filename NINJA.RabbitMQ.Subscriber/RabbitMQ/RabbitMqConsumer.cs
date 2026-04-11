using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using NINJA.RabbitMQ.Subscriber.RabbitMQ.Connection;
using NINJA.RabbitMQ.Subscriber.RabbitMQ.Strategies;
using System.Text;
using System.Collections.Generic;

namespace NINJA.RabbitMQ.Subscriber.RabbitMQ
{
    public class RabbitMqConsumer: IMessageConsumer, IDisposable
    {
        private readonly IRabbitMqConnection _connection;
        private IModel? _channel;
        private readonly IStreamOffsetStrategyFactory _strategyFactory;
        private string? _queueName;
        private readonly object _lock = new();
        public RabbitMqConsumer(IRabbitMqConnection connection,IStreamOffsetStrategyFactory strategyFactory)
        {
            _connection = connection;
            _strategyFactory = strategyFactory;
        }

        public void StartConsuming(string queueName,bool autoAck = false,Action<string>? messageHandler = null,
            string? deadLetterExchange = null)
        {
            _queueName = queueName;
            _channel = GetOrCreateChannel();

            // Declare classic queue with optional dead letter exchange
            var arguments = new Dictionary<string,object>();

            if (!string.IsNullOrEmpty(deadLetterExchange))
            {
                arguments.Add("x-dead-letter-exchange",deadLetterExchange);
            }

            _channel.QueueDeclare(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: arguments);

            _channel.BasicQos(0,1,false);

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.Received += async (sender,ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine($"Consumer Received Message {(string.IsNullOrEmpty(deadLetterExchange) ? "" : $"(DLX: {deadLetterExchange})")}");

                // Use the provided message handler if available, otherwise just log
                messageHandler?.Invoke(message);

                if (!autoAck)
                {
                    _channel.BasicAck(deliveryTag: ea.DeliveryTag,multiple: false);
                }

                await Task.Yield();
            };

            _channel.BasicConsume(
                queue: queueName,
                autoAck: autoAck,
                consumer: consumer);
        }

        public void StartConsumingQuorum(string queueName,bool autoAck = false,Action<string>? messageHandler = null,
            string deadLetterStrategy = "at-least-once",string overflow = "reject-publish",int initialGroupSize = 0,
            string? deadLetterExchange = null)
        {
            _queueName = queueName;
            _channel = GetOrCreateChannel();

            // Declare quorum queue with comprehensive arguments
            var arguments = new Dictionary<string,object>
            {
                {"x-queue-type", "quorum"},
                {"x-dead-letter-strategy", deadLetterStrategy},
                { "x-delivery-limit", 5 },
                {"x-overflow", overflow},
                { "x-single-active-consumer", true }
            };

            // Add dead letter exchange if specified
            if (!string.IsNullOrEmpty(deadLetterExchange))
            {
                string dlqName = $"{queueName}.dlq";
                _channel.ExchangeDeclare(
                    exchange: deadLetterExchange,
                    type: ExchangeType.Direct,
                    durable: true);
                arguments.Add("x-dead-letter-exchange",deadLetterExchange);
                _channel.QueueDeclare(queue: dlqName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false
                   );
                _channel.QueueBind(queue: dlqName,
                    exchange: deadLetterExchange,
                    routingKey: queueName);
            }

            // Add initial group size if specified (0 means use cluster default)
            if (initialGroupSize > 0)
            {
                arguments.Add("x-quorum-initial-group-size",initialGroupSize);
            }

            _channel.QueueDeclare(
                queue: queueName,
                durable: true,        // Must be true for quorum queues
                exclusive: false,
                autoDelete: false,    // Must be false for quorum queues
                arguments: arguments);

            _channel.BasicQos(0,1,false);

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.Received += async (sender,ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine($"Quorum Consumer Received Message (DLX: {deadLetterExchange ?? "None"}, DL Strategy: {deadLetterStrategy}, Overflow: {overflow})");

                try
                {
                    // Use the provided message handler if available, otherwise just log
                    messageHandler?.Invoke(message);
                    _channel.BasicAck(deliveryTag: ea.DeliveryTag,multiple: false);
                }
                catch (Exception)
                {
                    _channel.BasicNack(
                        deliveryTag: ea.DeliveryTag,
                        multiple: false,
                        requeue: true // retry
                        );
                    if (ea.BasicProperties.Headers != null && ea.BasicProperties.Headers.ContainsKey("x-death"))
                    {
                        Console.WriteLine("Message moved to DLQ");
                    }
                }

                await Task.Yield();
            };

            _channel.BasicConsume(
                queue: queueName,
                autoAck: autoAck,
                consumer: consumer);
        }

        public void StartConsumingStream(string streamName,Action<string>? messageHandler = null,
            long retentionSize = 0,TimeSpan? retentionTime = null,int maxSegmentSize = 0,
            string streamOffset = "last",ulong? specificOffset = null)
        {
            _queueName = streamName;
            _channel = GetOrCreateChannel();

            // Declare stream with retention configuration
            var arguments = new Dictionary<string,object>
            {
                {"x-queue-type", "stream"}
            };

            // Add retention size if specified (in bytes)
            if (retentionSize > 0)
            {
                arguments.Add("x-max-length-bytes",retentionSize);
            }

            // Add retention time if specified (using time format like "24h", "1d", "30m")
            if (retentionTime.HasValue && retentionTime.Value.TotalMilliseconds > 0)
            {
                string timeFormat = FormatTimeForStream(retentionTime.Value);
                arguments.Add("x-max-age",timeFormat);
            }

            // Add max segment size if specified (in bytes)
            if (maxSegmentSize > 0)
            {
                arguments.Add("x-stream-max-segment-size-bytes",maxSegmentSize);
            }

            _channel.QueueDeclare(
                queue: streamName,
                durable: true,        // Streams must be durable
                exclusive: false,
                autoDelete: false,    // Streams cannot be auto-delete
                arguments: arguments);

            // Streams REQUIRE a prefetch count — broker throws PRECONDITION_FAILED without it
            _channel.BasicQos(0, 100, false);

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.Received += async (sender,ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine($"Stream Consumer (offset: {streamOffset}) Received Message from '{streamName}' (Current Offset: {ea.DeliveryTag}, Retention: {(retentionSize > 0 ? $"{retentionSize} bytes" : "unlimited")}, Max Age: {(retentionTime.HasValue ? FormatTimeForStream(retentionTime.Value) : "unlimited")})");

                // Use the provided message handler if available, otherwise just log
                messageHandler?.Invoke(message);

                // Streams always require explicit acknowledgment
                _channel.BasicAck(deliveryTag: ea.DeliveryTag,multiple: false);

                await Task.Yield();
            };

            // Use Strategy pattern for stream offset configuration - SOLID approach
            IStreamOffsetStrategy strategy;
            if (streamOffset.ToLower() == "specific" || streamOffset.ToLower() == "offset")
            {
                strategy = _strategyFactory.CreateSpecificStrategy();
            }
            else
            {
                strategy = _strategyFactory.CreateStrategy(streamOffset);
            }

            var consumerArgs = strategy.GetConsumerArguments(streamOffset,specificOffset);

            _channel.BasicConsume(
                queue: streamName,
                autoAck: false,    // Streams should always use manual ack
                consumer: consumer,
                arguments: consumerArgs);
        }
        private IModel GetOrCreateChannel()
        {
            lock (_lock)
            {
                if (_channel == null || _channel.IsClosed)
                    _channel = _connection.Connection.CreateModel();
                return _channel;
            }
        }
        private static string FormatTimeForStream(TimeSpan timeSpan)
        {
            // RabbitMQ stream x-max-age format: "Y", "M", "D", "h", "m", "s" (single unit only)
            // Examples: "24h", "7D", "30m", "60s"

            if (timeSpan.TotalDays >= 1)
            {
                var days = (int)Math.Floor(timeSpan.TotalDays);
                return days == 1 ? "1D" : $"{days}D";
            }

            if (timeSpan.TotalHours >= 1)
            {
                var hours = (int)Math.Floor(timeSpan.TotalHours);
                return hours == 1 ? "1h" : $"{hours}h";
            }

            if (timeSpan.TotalMinutes >= 1)
            {
                var minutes = (int)Math.Floor(timeSpan.TotalMinutes);
                return minutes == 1 ? "1m" : $"{minutes}m";
            }

            // Default to seconds for very short durations
            var seconds = (int)Math.Max(1,Math.Floor(timeSpan.TotalSeconds));
            return seconds == 1 ? "1s" : $"{seconds}s";
        }

        public void StopConsuming()
        {
            _channel?.Close();
        }

        public void Dispose()
        {
            _channel?.Close();
            _channel?.Dispose();
        }
    }
}