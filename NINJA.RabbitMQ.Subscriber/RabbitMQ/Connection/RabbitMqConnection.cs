using RabbitMQ.Client;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Exceptions;
using System.Net.Sockets;
using Polly;

namespace NINJA.RabbitMQ.Subscriber.RabbitMQ.Connection
{
    public class RabbitMqConnection: IRabbitMqConnection, IDisposable
    {
        private IConnection? _connection;
        private readonly ConnectionFactory _factory;
        private readonly object _lock = new();
        private readonly ILogger<RabbitMqConnection> _logger;
        public RabbitMqConnection(IOptions<RabbitMqSettings> settings,ILogger<RabbitMqConnection> logger)
        {
            _logger = logger;
            _factory = new ConnectionFactory()
            {
                HostName = settings.Value.HostName,
                UserName = settings.Value.UserName,
                Password = settings.Value.Password,
                VirtualHost = settings.Value.VirtualHost,
                Port = settings.Value.Port,
                DispatchConsumersAsync = true,       // Always set this for async consumers
                AutomaticRecoveryEnabled = true,      // Built-in reconnect
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };
        }

        public IConnection Connection
        {
            get
            {
                lock (_lock)
                {
                    if (_connection == null || !_connection.IsOpen)
                        _connection = CreateConnectionWithRetry();
                    return _connection;
                }
            }
        }
        private IConnection CreateConnectionWithRetry()
        {
            var retryPolicy = Policy.Handle<BrokerUnreachableException>()
                .Or<SocketException>()
                .WaitAndRetry(
                    retryCount: 5,
                    sleepDurationProvider: attempt =>
                        TimeSpan.FromSeconds(Math.Pow(2,attempt)), // 2,4,8,16,32s
                    onRetry: (ex,delay,attempt,_) =>
                        _logger.LogWarning(
                            "RabbitMQ connection attempt {Attempt} failed. Retrying in {Delay}s. Error: {Error}",
                            attempt,delay.TotalSeconds,ex.Message)
                );

            return retryPolicy.Execute(() => _factory.CreateConnection());
        }
        public void Dispose()
        {
            _connection?.Close();
            _connection?.Dispose();
        }
    }
}
