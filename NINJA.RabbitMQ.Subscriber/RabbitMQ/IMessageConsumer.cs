namespace NINJA.RabbitMQ.Subscriber.RabbitMQ
{
    public interface IMessageConsumer
    {
        void StartConsuming(string queueName,bool autoAck = false,Action<string>? messageHandler = null,
            string? deadLetterExchange = null);

        void StartConsumingQuorum(string queueName,bool autoAck = false,Action<string>? messageHandler = null,
            string deadLetterStrategy = "at-least-once",string overflow = "drop-head",int initialGroupSize = 0,
            string? deadLetterExchange = null);

        // Uses the RabbitMQ Stream Protocol (port 5552) via RabbitMQ.Stream.Client.
        // Async because StreamSystem.Create and CreateRawConsumer are inherently async.
        Task StartConsumingStream(string streamName,Action<string>? messageHandler = null,
            long retentionSize = 0,TimeSpan? retentionTime = null,int maxSegmentSize = 0,
            string streamOffset = "last",ulong? specificOffset = null);

        // Binds a durable queue to the built-in firehose exchange (amq.rabbitmq.trace)
        // and prints a descriptive trace card for every intercepted message.
        //
        // Prerequisites — firehose must be switched on first:
        //   rabbitmqctl trace_on          (CLI)
        //   Management UI → Admin → Tracing → Add trace
        //
        // Routing-key pattern published by the broker:
        //   publish.<exchange>  — every message entering <exchange>
        //   deliver.<queue>     — every message leaving  <queue>
        //
        // Default values reproduce the task exactly:
        //   queue      = "tracer-qu"
        //   routingKey = "publish.amq.direct"   (trace all publishes to amq.direct)
        void StartConsumingTrace(string queueName = "tracer-qu",
            string routingKey = "publish.amq.direct");

        Task StopConsuming();
    }
}
