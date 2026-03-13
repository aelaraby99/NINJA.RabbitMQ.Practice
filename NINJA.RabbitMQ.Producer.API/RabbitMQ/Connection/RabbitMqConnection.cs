using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
namespace NINJA.RabbitMQ.Producer.API.RabbitMQ.Connection
{
    public class RabbitMqConnection: IRabbitMqConnection, IDisposable
    {
        private IConnection? _connection;
        public IConnection Connection => _connection!;
        private readonly RabbitMqSettings _settings;
        public RabbitMqConnection(IOptions<RabbitMqSettings> options)
        {
            _settings = options.Value;
            InitializeConnection();
        }

        private void InitializeConnection()
        {
            var factory = new ConnectionFactory()
            {
                HostName = _settings.HostName,
                Password = _settings.Password,
                VirtualHost = _settings.VirtualHost,
                UserName = _settings?.UserName,
                Port = _settings?.Port ?? 0
            };
            _connection = factory.CreateConnection();
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}