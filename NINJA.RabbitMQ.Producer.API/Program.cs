using Microsoft.EntityFrameworkCore;
using NINJA.RabbitMQ.Producer.API.Data;
using NINJA.RabbitMQ.Producer.API.RabbitMQ;
using NINJA.RabbitMQ.Producer.API.RabbitMQ.Connection;
using NINJA.RabbitMQ.Producer.API.Services;

namespace NINJA.RabbitMQ.Producer.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ── Options (validated eagerly — bad config fails at startup, not mid-request) ──
            builder.Services.AddOptions<RabbitMqSettings>()
                .BindConfiguration(RabbitMqSettings.SectionName)
                .ValidateDataAnnotations()   // honours [Required] / [Range] on RabbitMqSettings
                .ValidateOnStart();          // throws OptionsValidationException before first request

            // ── Infrastructure ────────────────────────────────────────────────────────────
            builder.Services.AddDbContext<OrderDbContext>(
                options => options.UseInMemoryDatabase("RabbitMQ"));

            // Singleton: one AMQP connection shared across all requests.
            builder.Services.AddSingleton<IRabbitMqConnection,RabbitMqConnection>();
            // Scoped: one channel per HTTP request (channel is cheap, per-request is safe).
            builder.Services.AddScoped<IMessageProducer,RabbitMqProducer>();

            // ── Application services ──────────────────────────────────────────────────────
            builder.Services.AddScoped<IOrderService,OrderService>();
            builder.Services.AddScoped<IWeatherForecastService,WeatherForecastService>();

            // ── ASP.NET Core pipeline ─────────────────────────────────────────────────────
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}
