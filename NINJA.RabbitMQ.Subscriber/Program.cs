using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using NINJA.RabbitMQ.Subscriber.RabbitMQ;
using NINJA.RabbitMQ.Subscriber.RabbitMQ.Connection;
using NINJA.RabbitMQ.Subscriber.Services;

namespace NINJA.RabbitMQ.Subscriber
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            builder.Configuration.AddJsonFile("appsettings.json",optional: false,reloadOnChange: true);

            builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMQ"));
            builder.Services.AddSingleton<IRabbitMqConnection,RabbitMqConnection>();
            builder.Services.AddScoped<IMessageConsumer,RabbitMqConsumer>();
            builder.Services.AddScoped<IWeatherForecastService,WeatherForecastService>();

            var host = builder.Build();
            using var scope = host.Services.CreateScope();
            var consumer = scope.ServiceProvider.GetRequiredService<IMessageConsumer>();
            var weatherService = scope.ServiceProvider.GetRequiredService<IWeatherForecastService>();

            Console.WriteLine("Starting RabbitMQ Subscriber...");
            Console.WriteLine("Press any key to stop.");

            // Use WeatherForecastService specifically for weather-forecasts queue
            consumer.StartConsuming("weather-forecasts",autoAck: false,messageHandler: weatherService.ProcessWeatherForecast);


            consumer.StartConsumingQuorum("critical-orders",
                autoAck: false,
                 messageHandler: msg =>
                 {
                     Console.WriteLine($"Processing critical order: {msg}");
                     if (msg.Contains("fail",StringComparison.OrdinalIgnoreCase))
                         throw new Exception("Simulated failure");
                 },
                 deadLetterExchange: "critical-orders.dlx"
                );
            consumer.StartConsuming("critical-orders.dlq",
                autoAck: false,messageHandler: msg =>
           {
               Console.WriteLine($"💀 DLQ Received: {msg}");
           });
            Console.ReadKey();
            consumer.StopConsuming();
        }
    }
}