namespace NINJA.RabbitMQ.Producer.API.Services
{
    public interface IWeatherForecastService
    {
        IEnumerable<WeatherForecast> GetWeather();
    }
}