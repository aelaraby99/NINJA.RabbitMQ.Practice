using RabbitMQ.Client;

namespace NINJA.RabbitMQ.Subscriber.RabbitMQ.Connection
{
    public interface IRabbitMqConnection
    {
        IConnection Connection { get; }
    }
}
