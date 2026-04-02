using System.Collections.Generic;

namespace NINJA.RabbitMQ.Subscriber.RabbitMQ.Strategies
{
    public class SpecificOffsetStrategy : IStreamOffsetStrategy
    {
        public Dictionary<string, object> GetConsumerArguments(string streamOffset, ulong? specificOffset)
        {
            if (!specificOffset.HasValue)
            {
                throw new ArgumentException("specificOffset must be provided for SpecificOffsetStrategy");
            }

            return new Dictionary<string, object>
            {
                {"x-stream-offset", specificOffset.Value}
            };
        }
    }
}
