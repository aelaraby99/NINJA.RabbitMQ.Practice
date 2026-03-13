
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

            // Add services to the container.
            builder.Services.AddDbContext<OrderDbContext>(options => options.UseInMemoryDatabase("RabbitMQ"));
            builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMQ"));
            builder.Services.AddScoped<IOrderService,OrderService>();
            builder.Services.AddScoped<IWeatherForecastService,WeatherForecastService>();
            builder.Services.AddSingleton<IRabbitMqConnection, RabbitMqConnection>();
            builder.Services.AddScoped<IMessageProducer,RabbitMqProducer>();



            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
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
