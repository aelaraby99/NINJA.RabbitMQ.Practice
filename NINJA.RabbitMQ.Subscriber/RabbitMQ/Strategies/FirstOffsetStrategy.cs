using RabbitMQ.Stream.Client;

namespace NINJA.RabbitMQ.Subscriber.RabbitMQ.Strategies
{
    // Replay all messages stored in the stream from the very beginning.
    public class FirstOffsetStrategy : IStreamOffsetStrategy
    {
        public IOffsetType GetOffsetType(ulong? specificOffset = null) => new OffsetTypeFirst();
    }
}
