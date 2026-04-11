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
        static void Main(string[] args)
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

            // Create separate consumer instances for each queue type
            var classicConsumer = scope.ServiceProvider.GetRequiredService<IMessageConsumer>();
            var quorumConsumer = scope.ServiceProvider.GetRequiredService<IMessageConsumer>();
            var streamConsumer = scope.ServiceProvider.GetRequiredService<IMessageConsumer>();
            var quorumDLXConsumer = scope.ServiceProvider.GetRequiredService<IMessageConsumer>();
            var originalConsumer = scope.ServiceProvider.GetRequiredService<IMessageConsumer>();
            var criticalOrdersConsumer = scope.ServiceProvider.GetRequiredService<IMessageConsumer>();
            var criticalOrdersDLQConsumer = scope.ServiceProvider.GetRequiredService<IMessageConsumer>();

            // Classic queue consumer - matches producer endpoint
            classicConsumer.StartConsuming("classic-weather-forecasts",autoAck: false,messageHandler: weatherService.ProcessWeatherForecast);

            // Quorum queue consumer - matches producer endpoint
            quorumConsumer.StartConsumingQuorum("quorum-weather-forecasts",
                autoAck: false,
                messageHandler: msg =>
                {
                    Console.WriteLine($" Quorum Processing: {msg}");
                    weatherService.ProcessWeatherForecast(msg);
                },
                deadLetterStrategy: "at-least-once",
                overflow: "reject-publish",
                initialGroupSize: 3
            );

            // Stream queue consumer - matches producer endpoint
            streamConsumer.StartConsumingStream("stream-weather-forecasts",
                messageHandler: msg =>
                {
                    Console.WriteLine($" Stream Processing: {msg}");
                    weatherService.ProcessWeatherForecast(msg);
                },
                retentionSize: 1073741824, // 1GB
                retentionTime: TimeSpan.FromHours(24), // 24h
                maxSegmentSize: 1048576, // 1MB
                streamOffset: "first" // Consume all existing messages
            );

            // Quorum with DLX consumer - matches producer endpoint
            quorumDLXConsumer.StartConsumingQuorum("quorum-dlx-weather",
                autoAck: false,
                messageHandler: msg =>
                {
                    Console.WriteLine($" Quorum+DLX Processing: {msg}");
                    weatherService.ProcessWeatherForecast(msg);
                },
                deadLetterStrategy: "at-most-once",
                overflow: "reject-publish-dlx",
                initialGroupSize: 3,
                deadLetterExchange: "weather-dlx"
            );

            // Also keep the original consumers for backward compatibility
            originalConsumer.StartConsuming("weather-forecasts",autoAck: false,messageHandler: weatherService.ProcessWeatherForecast);

            criticalOrdersConsumer.StartConsumingQuorum("critical-orders",
               autoAck: false,
                messageHandler: msg =>
                {
                    Console.WriteLine($"Processing critical order: {msg}");
                    if (msg.Contains("fail",StringComparison.OrdinalIgnoreCase))
                        throw new Exception("Simulated failure");
                },
                deadLetterExchange: "critical-orders.dlx"
               );
            criticalOrdersDLQConsumer.StartConsuming("critical-orders.dlq",
                autoAck: false,
                messageHandler: msg =>
                {
                    Console.WriteLine($"💀 DLQ Received: {msg}");
                });

            Console.WriteLine("All consumers started successfully!");
            Console.WriteLine("Active consumers:");
            Console.WriteLine("  - Classic: classic-weather-forecasts");
            Console.WriteLine("  - Quorum: quorum-weather-forecasts");
            Console.WriteLine("  - Stream: stream-weather-forecasts");
            Console.WriteLine("  - Quorum+DLX: quorum-dlx-weather");
            Console.WriteLine("  - Original: weather-forecasts");
            Console.WriteLine("  - Critical Orders: critical-orders");
            Console.WriteLine("  - Critical Orders DLQ: critical-orders.dlq");

            Console.ReadKey();

            // Stop all consumers
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