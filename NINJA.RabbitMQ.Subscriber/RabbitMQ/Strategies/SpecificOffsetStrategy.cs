using RabbitMQ.Stream.Client;

namespace NINJA.RabbitMQ.Subscriber.RabbitMQ.Strategies
{
    // Resume from a known message offset — useful for exactly-once or checkpoint-based consumers.
    public class SpecificOffsetStrategy : IStreamOffsetStrategy
    {
        public IOffsetType GetOffsetType(ulong? specificOffset = null)
        {
            if (!specificOffset.HasValue)
                throw new ArgumentException("specificOffset must be provided for SpecificOffsetStrategy");

            return new OffsetTypeOffset(specificOffset.Value);
        }
    }
}
