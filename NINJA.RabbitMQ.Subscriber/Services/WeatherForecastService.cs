using NINJA.RabbitMQ.Subscriber.Data;
using System.Collections.Generic;

namespace NINJA.RabbitMQ.Subscriber.Services
{
    public class WeatherForecastService : IWeatherForecastService
    {
        public void ProcessWeatherForecast(string message)
        {
            Console.WriteLine($"Processing weather forecast deserializing...");
            var forecast = System.Text.Json.JsonSerializer.Deserialize<IEnumerable<WeatherForecast>>(message)?.FirstOrDefault();
            // Add any additional processing logic here
            Console.WriteLine($"Date: {forecast?.Date}, TemperatureC: {forecast?.TemperatureC}, TemperatureF: {forecast?.TemperatureF}, Summary: {forecast?.Summary}");
        }
    }
}