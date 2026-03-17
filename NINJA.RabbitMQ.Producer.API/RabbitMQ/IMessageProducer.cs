namespace NINJA.RabbitMQ.Producer.API.RabbitMQ
{
    public interface IMessageProducer
    {
        void SendMessage<T>(T message,string queueName,string exchangeName = "",string routingKey = "",IDictionary<string,object> arguments = null);
    }
}