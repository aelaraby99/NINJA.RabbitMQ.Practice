using NINJA.RabbitMQ.Producer.API.RabbitMQ.Connection;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace NINJA.RabbitMQ.Producer.API.RabbitMQ
{
    public class RabbitMqProducer: IMessageProducer
    {
        private readonly IRabbitMqConnection _connection;
        public RabbitMqProducer(IRabbitMqConnection connection)
        {
            _connection = connection;
        }
        public void SendMessage<T>(T message,string queueName,string exchangeName = "",string routingKey = "")
        {
            using var channel = _connection.Connection.CreateModel();
            channel.QueueDeclare(queueName,durable: true,exclusive: false,autoDelete: false);
            // Enable publisher confirms to ensure message delivery
            channel.ConfirmSelect();

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            IBasicProperties properties = channel.CreateBasicProperties();
            properties.Persistent = true; // Make message persistent
            channel.BasicReturn += (sender,args) =>
            {
                // Log the returned message or handle it as needed
                Console.WriteLine($"Message returned: ReplyCode: {args.ReplyCode}, ReplyText: {args.ReplyText}");
            };
            channel.BasicAcks += (sender,ae) =>
            {
                // Log the acknowledgment or handle it as needed
                Console.WriteLine($"Message acknowledged: DeliveryTag: {ae.DeliveryTag}");
            };
            channel.BasicPublish(
                exchange: exchangeName,
                routingKey: string.IsNullOrEmpty(routingKey) ? queueName : routingKey,
                mandatory: true,
                basicProperties: properties,
                body: body);
        }
    }
}