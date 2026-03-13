using RabbitMQ.Client;

namespace NINJA.RabbitMQ.Producer.API.RabbitMQ.Connection
{
    public interface IRabbitMqConnection
    {
        IConnection Connection { get; }
    }
}
