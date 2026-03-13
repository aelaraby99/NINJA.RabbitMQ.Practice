using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using System.Text;
namespace AllInOne.RabbitMQ.Producer.Console_App
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("RabbitMQ App Started....");
            ServiceCollection services = new ServiceCollection();
            services.AddSingleton<IConnectionFactory>(sp =>
            {
                return new ConnectionFactory
                {
                    HostName = "localhost",
                    Port = 5672,
                    UserName = "guest",
                    Password = "guest",
                    AutomaticRecoveryEnabled = true
                };
            });
            services.AddSingleton<IConnection>(sp =>
            {
                IConnectionFactory factory = sp.GetRequiredService<IConnectionFactory>();
                return factory.CreateConnectionAsync().GetAwaiter().GetResult();
            });
            services.AddSingleton<TopicExchangeManager>();
            ServiceProvider provider = services.BuildServiceProvider();
            TopicExchangeManager manager = provider.GetRequiredService<TopicExchangeManager>();
            await manager.generateLogs_TopicExchange();
            await manager.readLogs_Queues();
        }
        static async void example1()
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = await factory.CreateConnectionAsync())
            using (var channel = await connection.CreateChannelAsync())
            {
                await channel.ExchangeDeclareAsync(
                    exchange: "logs",
                    type: ExchangeType.Fanout,
                    autoDelete: true,
                    durable: false
                    );
                var message = "Hello World!";
                var body = Encoding.UTF8.GetBytes(message);
                await channel.BasicPublishAsync(
                    exchange: "",
                    routingKey: "q1",
                    body: body
                    );
                Console.WriteLine($"[x] Sent '{message}'");
            }
        }
        static async void example2()
        {
            var factory = new ConnectionFactory()
            {
                HostName = "localhost",
                Port = 5672,
                UserName = "guest",
                Password = "guest"
            };
            using (var connection = await factory.CreateConnectionAsync())
            using (var channel = await connection.CreateChannelAsync())
            {
                await channel.QueueDeclareAsync(
                    queue: "hello",
                    durable: false,
                    exclusive: true,
                    autoDelete: false,
                    arguments: null);
                var message = "Hello World!";
                var body = Encoding.UTF8.GetBytes(message);
                await channel.BasicPublishAsync(
                     exchange: "",
                     routingKey: "hello",
                     body: body
                     );
                Console.WriteLine($"[x] Sent '{message}'");
                Console.WriteLine("Press Enter to exit");
                Console.ReadLine();
            }
        }
    }
}