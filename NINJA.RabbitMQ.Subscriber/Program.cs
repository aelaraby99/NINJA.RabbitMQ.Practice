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

            var consumer = host.Services.GetRequiredService<IMessageConsumer>();
            var weatherService = host.Services.GetRequiredService<IWeatherForecastService>();

            Console.WriteLine("Starting RabbitMQ Subscriber...");
            Console.WriteLine("Press any key to stop.");

            // Use WeatherForecastService specifically for weather-forecasts queue
            consumer.StartConsuming("weather-forecasts", autoAck: false, messageHandler: weatherService.ProcessWeatherForecast);

            Console.ReadKey();

            consumer.StopConsuming();
        }
    }
}