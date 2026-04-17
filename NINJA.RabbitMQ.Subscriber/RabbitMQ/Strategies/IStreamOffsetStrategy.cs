using RabbitMQ.Stream.Client;

namespace NINJA.RabbitMQ.Subscriber.RabbitMQ.Strategies
{
    public interface IStreamOffsetStrategy
    {
        IOffsetType GetOffsetType(ulong? specificOffset = null);
    }
}
