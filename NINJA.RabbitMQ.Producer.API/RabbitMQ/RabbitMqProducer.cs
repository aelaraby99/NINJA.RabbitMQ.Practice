using Microsoft.Extensions.Logging;
using NINJA.RabbitMQ.Producer.API.RabbitMQ.Connection;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace NINJA.RabbitMQ.Producer.API.RabbitMQ
{
    public class RabbitMqProducer : IMessageProducer
    {
        private readonly IRabbitMqConnection _connection;
        private readonly ILogger<RabbitMqProducer> _logger;

        public RabbitMqProducer(IRabbitMqConnection connection, ILogger<RabbitMqProducer> logger)
        {
            _connection = connection;
            _logger     = logger;
        }

        public void SendMessage<T>(T message, string queueName, string exchangeName = "",
            string routingKey = "", IDictionary<string, object>? arguments = null)
        {
            using var channel = _connection.Connection.CreateModel();
            // Publisher confirms: broker must ACK before WaitForConfirmsOrDie returns.
            channel.ConfirmSelect();

            channel.BasicReturn += (_, args) =>
                _logger.LogWarning(
                    "Message returned — exchange: '{Exchange}', routingKey: '{RoutingKey}', " +
                    "reply: {ReplyCode} {ReplyText}",
                    args.Exchange, args.RoutingKey, args.ReplyCode, args.ReplyText);

            channel.BasicAcks += (_, ae) =>
                _logger.LogDebug(
                    "Broker ACK — deliveryTag: {DeliveryTag}, multiple: {Multiple}",
                    ae.DeliveryTag, ae.Multiple);

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = channel.CreateBasicProperties();
            properties.Persistent       = true;                        // survives broker restart
            properties.ContentType      = "application/json";
            properties.ContentEncoding  = "utf-8";
            properties.Timestamp        = new AmqpTimestamp(
                DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            var effectiveRoutingKey = string.IsNullOrEmpty(routingKey) ? queueName : routingKey;

            channel.BasicPublish(
                exchange:        exchangeName,
                routingKey:      effectiveRoutingKey,
                mandatory:       true,
                basicProperties: properties,
                body:            body);

            // Blocks until the broker confirms delivery (or throws after 5 s).
            channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));

            _logger.LogInformation(
                "Published {Size} bytes — exchange: '{Exchange}', routingKey: '{RoutingKey}'",
                body.Length,
                string.IsNullOrEmpty(exchangeName) ? "(default)" : exchangeName,
                effectiveRoutingKey);
        }
    }
}