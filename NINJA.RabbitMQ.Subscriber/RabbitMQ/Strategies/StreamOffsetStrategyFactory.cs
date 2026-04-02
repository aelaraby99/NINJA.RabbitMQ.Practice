namespace NINJA.RabbitMQ.Subscriber.RabbitMQ.Strategies
{
    public class StreamOffsetStrategyFactory : IStreamOffsetStrategyFactory
    {
        public IStreamOffsetStrategy CreateStrategy(string streamOffset)
        {
            return streamOffset.ToLower() switch
            {
                "first" => new FirstOffsetStrategy(),
                "last" => new LastOffsetStrategy(),
                "next" => new NextOffsetStrategy(),
                _ => new LastOffsetStrategy() // Default to safe option
            };
        }

        public IStreamOffsetStrategy CreateSpecificStrategy()
        {
            return new SpecificOffsetStrategy();
        }
    }
}
