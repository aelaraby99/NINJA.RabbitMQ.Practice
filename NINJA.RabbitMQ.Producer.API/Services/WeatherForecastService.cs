
using NINJA.RabbitMQ.Producer.API.Data;
using NINJA.RabbitMQ.Producer.API.RabbitMQ;

namespace NINJA.RabbitMQ.Producer.API.Services
{
    public class WeatherForecastService: IWeatherForecastService
    {
        private readonly string[] Summaries = new[] { "Freezing","Bracing","Chilly","Cool","Mild","Warm","Balmy","Hot","Sweltering","Scorching" };
        private readonly IMessageProducer _messageProducer;

        public WeatherForecastService(IMessageProducer messageProducer)
        {
            _messageProducer = messageProducer;
        }
        private IEnumerable<WeatherForecast> GetWeatherForecasts()
        {
            return Enumerable.Range(1,5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20,55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
           .ToList();
        }
        public IEnumerable<WeatherForecast> GetWeather()
        {
            var weatherForecasts = GetWeatherForecasts();
            if (weatherForecasts is not null)
            {
                _messageProducer.SendMessage(weatherForecasts,"weather-forecasts",arguments: new Dictionary<string,object> { { "x-max-length",3 },{ "x-overflow","reject-publish" } });
                return weatherForecasts;
            }
            return Enumerable.Empty<WeatherForecast>();
        }
    }
}