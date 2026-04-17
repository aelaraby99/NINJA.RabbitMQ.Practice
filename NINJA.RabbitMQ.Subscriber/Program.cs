using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using NINJA.RabbitMQ.Subscriber.RabbitMQ;
using NINJA.RabbitMQ.Subscriber.RabbitMQ.Connection;
using NINJA.RabbitMQ.Subscriber.RabbitMQ.Strategies;
using NINJA.RabbitMQ.Subscriber.Services;

namespace NINJA.RabbitMQ.Subscriber
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            builder.Configuration.AddJsonFile("appsettings.json",optional: false,reloadOnChange: true);

            builder.Services.AddOptions<RabbitMqSettings>().BindConfiguration(RabbitMqSettings.SectionName).ValidateOnStart();
            builder.Services.AddSingleton<IRabbitMqConnection,RabbitMqConnection>();
            builder.Services.AddSingleton<IStreamOffsetStrategyFactory,StreamOffsetStrategyFactory>();
            builder.Services.AddTransient<IMessageConsumer,RabbitMqConsumer>();
            builder.Services.AddScoped<IWeatherForecastService,WeatherForecastService>();

            var host = builder.Build();
            using var scope = host.Services.CreateScope();
            var weatherService = scope.ServiceProvider.GetRequiredService<IWeatherForecastService>();

            Console.WriteLine("Starting RabbitMQ Subscriber...");
            Console.WriteLine("Press any key to stop.");

            // Each GetRequiredService<IMessageConsumer>() returns a fresh transient instance
            // so every consumer owns its own AMQP channel / stream connection.
            var classicConsumer = scope.ServiceProvider.GetRequiredService<IMessageConsumer>();
            var quorumConsumer = scope.ServiceProvider.GetRequiredService<IMessageConsumer>();
            var streamConsumer = scope.ServiceProvider.GetRequiredService<IMessageConsumer>();
            var quorumDLXConsumer = scope.ServiceProvider.GetRequiredService<IMessageConsumer>();
            var originalConsumer = scope.ServiceProvider.GetRequiredService<IMessageConsumer>();
            var criticalOrdersConsumer = scope.ServiceProvider.GetRequiredService<IMessageConsumer>();
            var criticalOrdersDLQConsumer = scope.ServiceProvider.GetRequiredService<IMessageConsumer>();

            // --- Classic queue -------------------------------------------------------
            classicConsumer.StartConsuming(
                "classic-weather-forecasts",
                autoAck: false,
                messageHandler: weatherService.ProcessWeatherForecast);

            // --- Quorum queue --------------------------------------------------------
            quorumConsumer.StartConsumingQuorum(
                "quorum-weather-forecasts",
                autoAck: false,
                messageHandler: msg =>
                {
                    Console.WriteLine($"[Quorum] Processing: {msg}");
                    weatherService.ProcessWeatherForecast(msg);
                },
                deadLetterStrategy: "at-least-once",
                overflow: "reject-publish",
                initialGroupSize: 3);

            // --- Stream queue  (RabbitMQ Stream Protocol — port 5552) -----------------
            // StartConsumingStream is async because StreamSystem.Create and
            // Consumer.Create are both async operations in RabbitMQ.Stream.Client.
            await streamConsumer.StartConsumingStream(
                "stream-weather-forecasts",
                messageHandler: msg =>
                {
                    Console.WriteLine($"[Stream] Processing: {msg}");
                    weatherService.ProcessWeatherForecast(msg);
                },
                retentionSize: 1_073_741_824,           // 1 GB
                retentionTime: TimeSpan.FromHours(24),  // 24 h
                maxSegmentSize: 1_048_576,              // 1 MB
                streamOffset: "first");                 // replay all stored messages
                                                        //await streamConsumer.StartConsumingStream(
                                                        //  "stream-weather-forecasts",
                                                        //  messageHandler: msg =>
                                                        //  {
                                                        //      Console.WriteLine($"[Stream] Processing: {msg}");
                                                        //      weatherService.ProcessWeatherForecast(msg);
                                                        //  },
                                                        //  retentionSize: 1_073_741_824,           // 1 GB
                                                        //  retentionTime: TimeSpan.FromHours(24),  // 24 h
                                                        //  maxSegmentSize: 1_048_576,              // 1 MB
                                                        //  streamOffset: "offset",
                                                        //  specificOffset : 4);      
                                                        // --- Quorum + DLX --------------------------------------------------------
            quorumDLXConsumer.StartConsumingQuorum(
                "quorum-dlx-weather",
                autoAck: false,
                messageHandler: msg =>
                {
                    Console.WriteLine($"[Quorum+DLX] Processing: {msg}");
                    weatherService.ProcessWeatherForecast(msg);
                },
                deadLetterStrategy: "at-most-once",
                overflow: "reject-publish-dlx",
                initialGroupSize: 3,
                deadLetterExchange: "weather-dlx");

            // --- Original (backward-compat) ------------------------------------------
            originalConsumer.StartConsuming(
                "weather-forecasts",
                autoAck: false,
                messageHandler: weatherService.ProcessWeatherForecast);

            // --- Critical orders (quorum + DLQ) --------------------------------------
            criticalOrdersConsumer.StartConsumingQuorum(
                "critical-orders",
                autoAck: false,
                messageHandler: msg =>
                {
                    Console.WriteLine($"[CriticalOrders] Processing: {msg}");
                    if (msg.Contains("fail",StringComparison.OrdinalIgnoreCase))
                        throw new Exception("Simulated failure");
                },
                deadLetterExchange: "critical-orders.dlx");

            criticalOrdersDLQConsumer.StartConsuming(
                "critical-orders.dlq",
                autoAck: false,
                messageHandler: msg => Console.WriteLine($"[DLQ] Received: {msg}"));

            Console.WriteLine("All consumers started successfully!");
            Console.WriteLine("Active consumers:");
            Console.WriteLine("  [AMQP]   classic-weather-forecasts");
            Console.WriteLine("  [AMQP]   quorum-weather-forecasts");
            Console.WriteLine("  [STREAM] stream-weather-forecasts  (port 5552, RabbitMQ.Stream.Client)");
            Console.WriteLine("  [AMQP]   quorum-dlx-weather");
            Console.WriteLine("  [AMQP]   weather-forecasts");
            Console.WriteLine("  [AMQP]   critical-orders");
            Console.WriteLine("  [AMQP]   critical-orders.dlq");

            Console.ReadKey();

            classicConsumer.StopConsuming();
            quorumConsumer.StopConsuming();
            streamConsumer.StopConsuming();
            quorumDLXConsumer.StopConsuming();
            originalConsumer.StopConsuming();
            criticalOrdersConsumer.StopConsuming();
            criticalOrdersDLQConsumer.StopConsuming();

            Console.WriteLine("All consumers stopped.");
        }
    }
}
