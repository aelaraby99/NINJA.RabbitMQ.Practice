using RabbitMQ.Stream.Client;

namespace NINJA.RabbitMQ.Subscriber.RabbitMQ.Strategies
{
    // Only consume messages published after this consumer attaches — skip all history.
    public class NextOffsetStrategy : IStreamOffsetStrategy
    {
        public IOffsetType GetOffsetType(ulong? specificOffset = null) => new OffsetTypeNext();
    }
}
