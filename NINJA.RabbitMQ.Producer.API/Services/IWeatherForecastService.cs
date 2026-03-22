using NINJA.RabbitMQ.Producer.API.Data;

namespace NINJA.RabbitMQ.Producer.API.Services
{
    public interface IWeatherForecastService
    {
        IEnumerable<WeatherForecast> GetWeather();
    }
}