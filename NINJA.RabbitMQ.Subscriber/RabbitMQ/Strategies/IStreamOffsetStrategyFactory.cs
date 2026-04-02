namespace NINJA.RabbitMQ.Subscriber.RabbitMQ.Strategies
{
    public interface IStreamOffsetStrategyFactory
    {
        IStreamOffsetStrategy CreateStrategy(string streamOffset);
        IStreamOffsetStrategy CreateSpecificStrategy();
    }
}
