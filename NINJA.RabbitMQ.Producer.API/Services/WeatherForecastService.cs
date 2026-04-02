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

        public IEnumerable<WeatherForecast> GetWeatherClassic()
        {
            var weatherForecasts = GetWeatherForecasts();
            if (weatherForecasts is not null)
            {
                // Classic queue - basic arguments
                _messageProducer.SendMessage(weatherForecasts, "classic-weather-forecasts");
                return weatherForecasts;
            }
            return Enumerable.Empty<WeatherForecast>();
        }

        public IEnumerable<WeatherForecast> GetWeatherQuorum()
        {
            var weatherForecasts = GetWeatherForecasts();
            if (weatherForecasts is not null)
            {
                // Quorum queue with specific arguments
                var quorumArguments = new Dictionary<string, object>
                {
                    { "x-queue-type", "quorum" },
                    { "x-dead-letter-strategy", "at-least-once" },
                    { "x-overflow", "reject-publish" },
                    { "x-delivery-limit", 5 },
                    { "x-single-active-consumer", true }
                };

                _messageProducer.SendMessage(weatherForecasts, "quorum-weather-forecasts", arguments: quorumArguments);
                return weatherForecasts;
            }
            return Enumerable.Empty<WeatherForecast>();
        }

        public IEnumerable<WeatherForecast> GetWeatherStream()
        {
            var weatherForecasts = GetWeatherForecasts();
            if (weatherForecasts is not null)
            {
                // Stream queue with retention arguments
                var streamArguments = new Dictionary<string, object>
                {
                    { "x-queue-type", "stream" },
                    { "x-max-length-bytes", 1073741824 }, // 1GB retention
                    { "x-max-age", "24h" }, // 24 hours
                    { "x-stream-max-segment-size-bytes", 1048576 } // 1MB segments
                };

                _messageProducer.SendMessage(weatherForecasts, "stream-weather-forecasts", arguments: streamArguments);
                return weatherForecasts;
            }
            return Enumerable.Empty<WeatherForecast>();
        }

        public IEnumerable<WeatherForecast> GetWeatherQuorumWithDLX()
        {
            var weatherForecasts = GetWeatherForecasts();
            if (weatherForecasts is not null)
            {
                // Quorum queue with dead letter exchange
                var quorumDLXArguments = new Dictionary<string, object>
                {
                    { "x-queue-type", "quorum" },
                    { "x-dead-letter-strategy", "at-most-once" },
                    { "x-overflow", "reject-publish-dlx" },
                    { "x-delivery-limit", 3 },
                    { "x-dead-letter-exchange", "weather-dlx" }
                };

                _messageProducer.SendMessage(weatherForecasts, "quorum-dlx-weather", arguments: quorumDLXArguments);
                return weatherForecasts;
            }
            return Enumerable.Empty<WeatherForecast>();
        }
    }
}