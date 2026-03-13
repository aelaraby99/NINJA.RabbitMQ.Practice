using Microsoft.AspNetCore.Mvc;
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
        public async Task<IEnumerable<WeatherForecast>> Get()
        {
            return await weatherForecastService.GetWeather();
        }
    }
}