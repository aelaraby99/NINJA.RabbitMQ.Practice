namespace NINJA.RabbitMQ.Subscriber.Services
{
    public class WeatherForecastService : IWeatherForecastService
    {
        public void ProcessWeatherForecast(string message)
        {
            Console.WriteLine($"Processing weather forecast: {message}");
            // Add any additional processing logic here
        }
    }
}