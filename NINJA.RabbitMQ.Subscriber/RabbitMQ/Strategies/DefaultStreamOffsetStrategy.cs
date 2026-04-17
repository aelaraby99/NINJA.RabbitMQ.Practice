using RabbitMQ.Stream.Client;

namespace NINJA.RabbitMQ.Subscriber.RabbitMQ.Strategies
{
    // Fallback strategy: resolves to a specific offset when provided, otherwise defaults to Last.
    public class DefaultStreamOffsetStrategy : IStreamOffsetStrategy
    {
        public IOffsetType GetOffsetType(ulong? specificOffset = null)
        {
            return specificOffset.HasValue
                ? new OffsetTypeOffset(specificOffset.Value)
                : new OffsetTypeLast();
        }
    }
}
