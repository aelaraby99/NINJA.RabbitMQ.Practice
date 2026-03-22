using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using NINJA.RabbitMQ.Subscriber.RabbitMQ.Connection;
using System.Text;
namespace NINJA.RabbitMQ.Subscriber.RabbitMQ
{
    public class RabbitMqConsumer : IMessageConsumer, IDisposable
    {
        private readonly IRabbitMqConnection _connection;
        private readonly IModel _channel;
        private string _queueName;

        public RabbitMqConsumer(IRabbitMqConnection connection)
        {
            _connection = connection;
            _channel = _connection.Connection.CreateModel();
        }

        public void StartConsuming(string queueName, bool autoAck = false)
        {
            _queueName = queueName;
            // Declare queue
            _channel.QueueDeclare(
                queue: queueName,
                durable: false,
                exclusive: false,
                autoDelete: false);
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (sender, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine($"Consumer Received Message: {message}");
                if (!autoAck)
                {
                    _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
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