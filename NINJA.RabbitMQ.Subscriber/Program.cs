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
            channel.QueueDeclare(queue: "orders",exclusive: false);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (sender,ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine($"Received Message: {message}");
                channel.BasicAck(deliveryTag: ea.DeliveryTag,multiple: false);
                //channel.BasicNack(deliveryTag: ea.DeliveryTag,multiple: false,requeue: true);
            };
            channel.BasicConsume(
                queue: "orders",
                autoAck: false,
                consumer: consumer);

            Console.ReadKey();
        }
    }
}