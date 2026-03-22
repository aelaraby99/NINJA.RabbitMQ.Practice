using RabbitMQ.Client;
using Microsoft.Extensions.Options;

namespace NINJA.RabbitMQ.Subscriber.RabbitMQ.Connection
{
    public class RabbitMqConnection : IRabbitMqConnection, IDisposable
    {
        private readonly IConnection _connection;
        private readonly ConnectionFactory _factory;

        public RabbitMqConnection(IOptions<RabbitMqSettings> settings)
        {
            _factory = new ConnectionFactory()
            {
                HostName = settings.Value.HostName,
                UserName = settings.Value.UserName,
                Password = settings.Value.Password,
                VirtualHost = settings.Value.VirtualHost,
                Port = settings.Value.Port
            };
            _connection = _factory.CreateConnection();
        }

        public IConnection Connection => _connection;

        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}
