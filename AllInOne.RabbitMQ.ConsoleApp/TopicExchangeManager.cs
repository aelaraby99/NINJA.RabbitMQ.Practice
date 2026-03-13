using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace AllInOne.RabbitMQ.Producer.Console_App
{
    public class TopicExchangeManager
    {
        private readonly IConnection _connection;
        public TopicExchangeManager(IConnection connection)
        {
            _connection = connection;
        }
        public async Task generateLogs_TopicExchange()
        {
            using IChannel channel = await _connection.CreateChannelAsync();
            for (int i = 0;i < 100;i++)
            {
                Thread.Sleep(100);
                if (i % 2 == 0)
                {
                    var message = $"Log info '{i}'";
                    var body = Encoding.UTF8.GetBytes(message);
                    await channel.BasicPublishAsync(
                        exchange: "DeadExchange",
                        routingKey: "log.info",
                        body: body
                        );
                }
                else
                {
                    var message = $"Log error '{i}'";
                    var body = Encoding.UTF8.GetBytes(message);
                    await channel.BasicPublishAsync(
                        exchange: "DeadExchange",
                        routingKey: "log.error",
                        body: body
                        );

                }
                Console.Write($"{i}, ");
            }
            Console.WriteLine("Press Enter to exit");
            Console.ReadLine();
        }
        public async Task readLogs_Queues()
        {
            using var channel = await _connection.CreateChannelAsync();
            // Create a single async consumer
            var consumer = new AsyncEventingBasicConsumer(channel);
            // Attach the handler BEFORE subscribing to any queue
            consumer.ReceivedAsync += async (model,ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.ForegroundColor = ea.ConsumerTag switch
                {
                    "ERROR" => ConsoleColor.Red,
                    "INFO" => ConsoleColor.Green,
                    _ => ConsoleColor.White
                };
                if (ea.ConsumerTag == "ERROR")
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    await channel.BasicRejectAsync(deliveryTag: ea.DeliveryTag, requeue: false);
                    Console.WriteLine($"ERROR message rejected to DLX: {message}");
                }
                else
                {
                    Console.WriteLine($"Message: {message} has been consumed by '{ea.ConsumerTag}'");
                }
                Console.ResetColor();
                await Task.Yield();
            };
            // Subscribe to all queues AFTER attaching handler
            await channel.BasicConsumeAsync(queue: "QError",autoAck: false,consumerTag: "ERROR",consumer: consumer);
            await channel.BasicConsumeAsync(queue: "QInfo",autoAck: true,consumerTag: "INFO",consumer: consumer);
            await channel.BasicConsumeAsync(queue: "QLog",autoAck: true,consumerTag: "All",consumer: consumer);
            
            await channel.BasicConsumeAsync(queue: "QDead",autoAck: false,consumerTag: "Dead",consumer: consumer);

            Console.WriteLine("Press [Enter] to exit");
            Console.ReadLine();
        }
    }
}