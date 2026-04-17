namespace NINJA.RabbitMQ.Subscriber.RabbitMQ
{
    public interface IMessageConsumer
    {
        void StartConsuming(string queueName, bool autoAck = false, Action<string>? messageHandler = null,
            string? deadLetterExchange = null);

        void StartConsumingQuorum(string queueName, bool autoAck = false, Action<string>? messageHandler = null,
            string deadLetterStrategy = "at-least-once", string overflow = "drop-head", int initialGroupSize = 0,
            string? deadLetterExchange = null);

        // Uses the RabbitMQ Stream Protocol (port 5552) via RabbitMQ.Stream.Client.
        // Async because StreamSystem.Create and CreateRawConsumer are inherently async.
        Task StartConsumingStream(string streamName, Action<string>? messageHandler = null,
            long retentionSize = 0, TimeSpan? retentionTime = null, int maxSegmentSize = 0,
            string streamOffset = "last", ulong? specificOffset = null);

        Task StopConsuming();
    }
}
