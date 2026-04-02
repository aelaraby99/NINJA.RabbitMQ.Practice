using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using NINJA.RabbitMQ.Subscriber.RabbitMQ.Connection;
using System.Text;

namespace NINJA.RabbitMQ.Subscriber.RabbitMQ
{
    public class RabbitMqConsumer: IMessageConsumer, IDisposable
    {
        private readonly IRabbitMqConnection _connection;
        private readonly IModel _channel;
        private string _queueName;

        public RabbitMqConsumer(IRabbitMqConnection connection)
        {
            _connection = connection;
            _channel = _connection.Connection.CreateModel();
        }

        public void StartConsuming(string queueName,bool autoAck = false,Action<string>? messageHandler = null,
            string? deadLetterExchange = null)
        {
            _queueName = queueName;

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

            _channel.BasicQos(0, 1, false);

            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += (sender,ea) =>
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

            _channel.BasicQos(0, 1, false);

            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += (sender,ea) =>
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
            };

            _channel.BasicConsume(
                queue: queueName,
                autoAck: autoAck,
                consumer: consumer);
        }

        public void StopConsuming()
        {
            _channel?.Close();
        }

        public void Dispose()
        {
            _channel?.Dispose();
        }
    }
}