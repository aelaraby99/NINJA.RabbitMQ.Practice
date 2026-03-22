namespace NINJA.RabbitMQ.Subscriber.Services
{
    public interface IWeatherForecastService
    {
        void ProcessWeatherForecast(string message);
    }
}
