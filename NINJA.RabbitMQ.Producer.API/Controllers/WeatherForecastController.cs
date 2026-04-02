using Microsoft.AspNetCore.Mvc;
using NINJA.RabbitMQ.Producer.API.Data;
using NINJA.RabbitMQ.Producer.API.Services;
namespace NINJA.RabbitMQ.Producer.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController: ControllerBase
    {
        private readonly IWeatherForecastService weatherForecastService;

        public WeatherForecastController(IWeatherForecastService weatherForecastService)
        {
            this.weatherForecastService = weatherForecastService;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<WeatherForecast> Get()
        {
            return weatherForecastService.GetWeather();
        }

        [HttpGet("classic", Name = "GetWeatherForecastClassic")]
        public IEnumerable<WeatherForecast> GetClassic()
        {
            return weatherForecastService.GetWeatherClassic();
        }

        [HttpGet("quorum", Name = "GetWeatherForecastQuorum")]
        public IEnumerable<WeatherForecast> GetQuorum()
        {
            return weatherForecastService.GetWeatherQuorum();
        }

        [HttpGet("stream", Name = "GetWeatherForecastStream")]
        public IEnumerable<WeatherForecast> GetStream()
        {
            return weatherForecastService.GetWeatherStream();
        }

        [HttpGet("quorum-dlx", Name = "GetWeatherForecastQuorumDLX")]
        public IEnumerable<WeatherForecast> GetQuorumWithDLX()
        {
            return weatherForecastService.GetWeatherQuorumWithDLX();
        }
    }
}