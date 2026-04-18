using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using NINJA.RabbitMQ.Subscriber.RabbitMQ;
using NINJA.RabbitMQ.Subscriber.RabbitMQ.Connection;
using NINJA.RabbitMQ.Subscriber.RabbitMQ.Strategies;
using NINJA.RabbitMQ.Subscriber.Services;

namespace NINJA.RabbitMQ.Subscriber
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            builder.Configuration.AddJsonFile("appsettings.json",optional: false,reloadOnChange: true);

            builder.Services.AddOptions<RabbitMqSettings>().BindConfiguration(RabbitMqSettings.SectionName).ValidateOnStart();
            builder.Services.AddSingleton<IRabbitMqConnection,RabbitMqConnection>();
            builder.Services.AddSingleton<IStreamOffsetStrategyFactory,StreamOffsetStrategyFactory>();
            builder.Services.AddTransient<IMessageConsumer,RabbitMqConsumer>();
            builder.Services.AddScoped<IWeatherForecastService,WeatherForecastService>();

            var host = builder.Build();

            // CreateAsyncScope() returns AsyncServiceScope which implements IAsyncDisposable.
            // This allows 'await using' and ensures the container calls DisposeAsync()
            // (not Dispose()) on tracked transient services like RabbitMqConsumer.
            // CreateScope() + plain 'using' would throw InvalidOperationException because
            // the sync dispose path cannot handle IAsyncDisposable-only services.
            await using var scope = host.Services.CreateAsyncScope();
            var weatherService = scope.ServiceProvider.GetRequiredService<IWeatherForecastService>();

            Console.WriteLine("Starting RabbitMQ Subscriber...");
            Console.WriteLine("Press any key to stop.");

            // Each GetRequiredService<IMessageConsumer>() returns a fresh transient instance
            // so every consumer owns its own AMQP channel / stream connection.
            var classicConsumer = scope.ServiceProvider.GetRequiredService<IMessageConsumer>();
            var quorumConsumer = scope.ServiceProvider.GetRequiredService<IMessageConsumer>();
            var streamConsumer = scope.ServiceProvider.GetRequiredService<IMessageConsumer>();
            var quorumDLXConsumer = scope.ServiceProvider.GetRequiredService<IMessageConsumer>();
            var originalConsumer = scope.ServiceProvider.GetRequiredService<IMessageConsumer>();
            var criticalOrdersConsumer = scope.ServiceProvider.GetRequiredService<IMessageConsumer>();
            var criticalOrdersDLQConsumer = scope.ServiceProvider.GetRequiredService<IMessageConsumer>();
            var tracerConsumer = scope.ServiceProvider.GetRequiredService<IMessageConsumer>();

            // --- Classic queue -------------------------------------------------------
            classicConsumer.StartConsuming(
                "classic-weather-forecasts",
                autoAck: false,
                messageHandler: weatherService.ProcessWeatherForecast);

            // --- Quorum queue --------------------------------------------------------
            quorumConsumer.StartConsumingQuorum(
                "quorum-weather-forecasts",
                autoAck: false,
                messageHandler: msg =>
                {
                    Console.WriteLine($"[Quorum] Processing: {msg}");
                    weatherService.ProcessWeatherForecast(msg);
                },
                deadLetterStrategy: "at-least-once",
                overflow: "reject-publish",
                initialGroupSize: 3);

            // --- Stream queue  (RabbitMQ Stream Protocol — port 5552) -----------------
            // StartConsumingStream is async because StreamSystem.Create and
            // Consumer.Create are both async operations in RabbitMQ.Stream.Client.
            await streamConsumer.StartConsumingStream(
                "stream-weather-forecasts",
                messageHandler: msg =>
                {
                    Console.WriteLine($"[Stream] Processing: {msg}");
                    weatherService.ProcessWeatherForecast(msg);
                },
                retentionSize: 1_073_741_824,          // 1 GB
                retentionTime: TimeSpan.FromHours(24), // 24 h
                maxSegmentSize: 1_048_576,             // 1 MB
                streamOffset: "first");                // replay all stored messages

            // --- Quorum + DLX --------------------------------------------------------
            quorumDLXConsumer.StartConsumingQuorum(
                "quorum-dlx-weather",
                autoAck: false,
                messageHandler: msg =>
                {
                    Console.WriteLine($"[Quorum+DLX] Processing: {msg}");
                    weatherService.ProcessWeatherForecast(msg);
                },
                deadLetterStrategy: "at-most-once",
                overflow: "reject-publish-dlx",
                initialGroupSize: 3,
                deadLetterExchange: "weather-dlx");

            // --- Original (backward-compat) ------------------------------------------
            originalConsumer.StartConsuming(
                "weather-forecasts",
                autoAck: false,
                messageHandler: weatherService.ProcessWeatherForecast);

            // --- Critical orders (quorum + DLQ) --------------------------------------
            criticalOrdersConsumer.StartConsumingQuorum(
                "critical-orders",
                autoAck: false,
                messageHandler: msg =>
                {
                    Console.WriteLine($"[CriticalOrders] Processing: {msg}");
                    if (msg.Contains("fail",StringComparison.OrdinalIgnoreCase))
                        throw new Exception("Simulated failure");
                },
                deadLetterExchange: "critical-orders.dlx");

            criticalOrdersDLQConsumer.StartConsuming(
                "critical-orders.dlq",
                autoAck: false,
                messageHandler: msg => Console.WriteLine($"[DLQ] Received: {msg}"));

            // --- Firehose tracer (amq.rabbitmq.trace) ----------------------------
            // Intercepts every message published to amq.direct and prints a
            // detailed trace card without affecting the original message flow.
            //
            //  Prerequisite — run once on the broker before starting:
            //        rabbitmqctl trace_on
            //     Or: Management UI → Admin → Tracing → Add trace
            //
            // The queue "tracer-qu" is declared and auto-bound here; the firehose
            // exchange (amq.rabbitmq.trace) is a system exchange — always present.
            tracerConsumer.StartConsumingTrace(
                queueName: "tracer-qu",
                routingKey: "publish.#");

            //tracerConsumer.StartConsumingTrace(
            //    queueName: "tracer-qu",
            //    routingKey: "deliver.#");

            Console.WriteLine("All consumers started successfully!");
            Console.WriteLine("Active consumers:");
            Console.WriteLine("  [AMQP]   classic-weather-forecasts");
            Console.WriteLine("  [AMQP]   quorum-weather-forecasts");
            Console.WriteLine("  [STREAM] stream-weather-forecasts  (port 5552, RabbitMQ.Stream.Client)");
            Console.WriteLine("  [AMQP]   quorum-dlx-weather");
            Console.WriteLine("  [AMQP]   weather-forecasts");
            Console.WriteLine("  [AMQP]   critical-orders");
            Console.WriteLine("  [AMQP]   critical-orders.dlq");
            Console.WriteLine("  [TRACE]  tracer-qu  ← amq.rabbitmq.trace / publish.amq.direct  (firehose)");

            Console.ReadKey();

            // StopConsuming() delegates to DisposeAsync() — each consumer does a
            // graceful protocol-level shutdown before releasing its connection.
            await classicConsumer.StopConsuming();
            await quorumConsumer.StopConsuming();
            await streamConsumer.StopConsuming();
            await quorumDLXConsumer.StopConsuming();
            await originalConsumer.StopConsuming();
            await criticalOrdersConsumer.StopConsuming();
            await criticalOrdersDLQConsumer.StopConsuming();
            await tracerConsumer.StopConsuming();

            Console.WriteLine("All consumers stopped.");
        }
    }
}
