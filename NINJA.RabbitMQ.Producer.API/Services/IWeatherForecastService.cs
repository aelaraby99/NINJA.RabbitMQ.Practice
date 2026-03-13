namespace NINJA.RabbitMQ.Producer.API.Services
{
    public interface IWeatherForecastService
    {
        Task<IEnumerable<WeatherForecast>> GetWeather();
    }
}