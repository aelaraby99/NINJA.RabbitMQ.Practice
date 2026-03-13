namespace NINJA.RabbitMQ.Producer.API.Data
{
    public class Order
    {
        public int Id { get; set; }
        public string? ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}