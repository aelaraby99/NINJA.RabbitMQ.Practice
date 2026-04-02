using System.Collections.Generic;

namespace NINJA.RabbitMQ.Subscriber.RabbitMQ.Strategies
{
    public class DefaultStreamOffsetStrategy : IStreamOffsetStrategy
    {
        public Dictionary<string, object> GetConsumerArguments(string streamOffset, ulong? specificOffset)
        {
            var consumerArgs = new Dictionary<string, object>();
            
            switch (streamOffset.ToLower())
            {
                case "first":
                    consumerArgs.Add("x-stream-offset", "first");
                    break;
                case "last":
                    consumerArgs.Add("x-stream-offset", "last");
                    break;
                case "next":
                    consumerArgs.Add("x-stream-offset", "next");
                    break;
                case "specific":
                case "offset":
                    if (specificOffset.HasValue)
                    {
                        consumerArgs.Add("x-stream-offset", specificOffset.Value);
                    }
                    else
                    {
                        throw new ArgumentException("specificOffset must be provided when streamOffset is 'specific' or 'offset'");
                    }
                    break;
                default:
                    if (specificOffset.HasValue)
                    {
                        consumerArgs.Add("x-stream-offset", specificOffset.Value);
                    }
                    else
                    {
                        consumerArgs.Add("x-stream-offset", "last"); // Default to safe option
                    }
                    break;
            }

            return consumerArgs;
        }
    }
}
