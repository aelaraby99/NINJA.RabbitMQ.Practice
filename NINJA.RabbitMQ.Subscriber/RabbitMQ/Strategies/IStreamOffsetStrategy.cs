namespace NINJA.RabbitMQ.Subscriber.RabbitMQ.Strategies
{
    public interface IStreamOffsetStrategy
    {
        Dictionary<string, object> GetConsumerArguments(string streamOffset, ulong? specificOffset);
    }
}
