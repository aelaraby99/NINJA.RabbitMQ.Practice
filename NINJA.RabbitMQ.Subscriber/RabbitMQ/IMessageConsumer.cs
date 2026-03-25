namespace NINJA.RabbitMQ.Subscriber.RabbitMQ
{
    public interface IMessageConsumer
    {
        void StartConsuming(string queueName, bool autoAck = false, Action<string>? messageHandler = null, 
            string? deadLetterExchange = null);
        void StartConsumingQuorum(string queueName, bool autoAck = false, Action<string>? messageHandler = null, 
            string deadLetterStrategy = "at-least-once", string overflow = "drop-head", int initialGroupSize = 0,
            string? deadLetterExchange = null);
        void StopConsuming();
    }
}
