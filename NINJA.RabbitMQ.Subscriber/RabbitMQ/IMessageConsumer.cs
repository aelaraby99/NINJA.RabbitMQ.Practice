namespace NINJA.RabbitMQ.Subscriber.RabbitMQ
{
    public interface IMessageConsumer
    {
        void StartConsuming(string queueName, bool autoAck = false, Action<string>? messageHandler = null);
        void StopConsuming();
    }
}
