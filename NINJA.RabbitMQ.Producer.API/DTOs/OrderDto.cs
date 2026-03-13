namespace NINJA.RabbitMQ.Producer.API.DTOs
{
    public class OrderDto
    {
        public string? ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}