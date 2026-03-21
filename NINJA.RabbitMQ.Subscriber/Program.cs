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
            //channel.BasicQos(prefetchSize: 0,prefetchCount: 1,global: false); // Set prefetch count to 1 to allow processing of 1 messages at a time
            //channel.QueueDeclare(
            //    queue: "orders",
            //    durable: true,
            //    exclusive: false,
            //    autoDelete: true);
            channel.QueueDeclare(
                queue: "weather-forecasts",
                durable: false,
                exclusive: false,
                autoDelete: false);
            //channel.QueueBind(
            //    queue: "weather-forecasts",
            //    exchange: "weather-exchange",
            //    routingKey: "forecast.*");
            //for (int i = 1;i <= 3;i++)
            //{
            // fair dispatch
            var consumer = new EventingBasicConsumer(channel);
            //var consumerId = i;
            consumer.Received += (sender,ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                //Console.WriteLine($"Consumer {consumerId} Received Message: {message}");
                Console.WriteLine($"Consumer Received Message: {message}");
                //Thread.Sleep(1000); // Simulate processing time
                channel.BasicAck(deliveryTag: ea.DeliveryTag,multiple: false);
                //channel.BasicNack(deliveryTag: ea.DeliveryTag,multiple: false,requeue: true); = channel.BasicReject(deliveryTag: ea.DeliveryTag,requeue: true);
            };
            //var argys = new Dictionary<string,object>();
            //if(i == 1)
            //    argys.Add("x-priority",10);
            //if(i == 2)
            //    argys.Add("x-priority",5);
            //channel.BasicConsume(
            //    queue: "orders",
            //    autoAck: false,
            //    consumer: consumer);
            channel.BasicConsume(
               queue: "weather-forecasts",
               autoAck: false,
               //arguments:argys,
               consumer: consumer);
            // }
            Console.ReadKey();
        }
    }
}