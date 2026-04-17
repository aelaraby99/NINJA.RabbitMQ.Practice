using RabbitMQ.Stream.Client;

namespace NINJA.RabbitMQ.Subscriber.RabbitMQ.Strategies
{
    // Start at the last available chunk — get the tail of historical data then continue live.
    public class LastOffsetStrategy : IStreamOffsetStrategy
    {
        public IOffsetType GetOffsetType(ulong? specificOffset = null) => new OffsetTypeLast();
    }
}
