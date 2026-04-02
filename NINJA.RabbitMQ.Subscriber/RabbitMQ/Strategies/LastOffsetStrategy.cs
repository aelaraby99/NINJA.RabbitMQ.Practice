using System.Collections.Generic;

namespace NINJA.RabbitMQ.Subscriber.RabbitMQ.Strategies
{
    public class LastOffsetStrategy : IStreamOffsetStrategy
    {
        public Dictionary<string, object> GetConsumerArguments(string streamOffset, ulong? specificOffset)
        {
            return new Dictionary<string, object>
            {
                {"x-stream-offset", "last"}
            };
        }
    }
}
