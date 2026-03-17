using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
namespace NINJA.RabbitMQ.Subscriber
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var factory = new ConnectionFactory()
            {
                HostName = "localhost",
                UserName = "guest",
                Password = "guest",
                Port = 5672
            };
            var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();
            channel.QueueDeclare(
                queue: "orders",
                durable: true,
                exclusive: false,
                autoDelete: true);
            channel.QueueDeclare(
                queue: "weather-forecasts",
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: new Dictionary<string,object>
                {
                    { "x-max-length", 3 }, // Set maximum queue length to 3 messages
                    { "x-overflow", "drop-head" } // Drop the oldest message in the queue when the maximum length is reached
                }
                );
            channel.BasicQos(prefetchSize: 0,prefetchCount: 3,global: false); // Set prefetch count to 2 to allow processing of 2 messages at a time
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (sender,ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine($"Received Message: {message}");
                channel.BasicAck(deliveryTag: ea.DeliveryTag,multiple: false);
                //channel.BasicNack(deliveryTag: ea.DeliveryTag,multiple: false,requeue: true); = channel.BasicReject(deliveryTag: ea.DeliveryTag,requeue: true);
            };
            channel.BasicConsume(
                queue: "orders",
                autoAck: false,
                consumer: consumer);
            channel.BasicConsume(
               queue: "weather-forecasts",
               autoAck: false,
               consumer: consumer);
            Console.ReadKey();
        }
    }
}