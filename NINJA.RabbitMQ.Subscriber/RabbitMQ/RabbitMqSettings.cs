using System.ComponentModel.DataAnnotations;

namespace NINJA.RabbitMQ.Subscriber.RabbitMQ
{
    public class RabbitMqSettings
    {
        public const string SectionName = "RabbitMQ";
        [Required(ErrorMessage = "RabbitMQ HostName is required")]
        public string HostName { get; set; } = string.Empty;
        [Required(ErrorMessage = "RabbitMQ UserName is required")]
        public string UserName { get; set; } = string.Empty;
        [Required(ErrorMessage = "RabbitMQ Password is required")]
        public string Password { get; set; } = string.Empty;
        public string VirtualHost { get; set; } = "/";
        [Range(1,65535,ErrorMessage = "Port must be between 1 and 65535")]
        public int Port { get; set; } = 5672;
    }
}