using NINJA.RabbitMQ.Producer.API.Data;
using NINJA.RabbitMQ.Producer.API.DTOs;
using NINJA.RabbitMQ.Producer.API.RabbitMQ;

namespace NINJA.RabbitMQ.Producer.API.Services
{
    public class OrderService: IOrderService
    {
        private readonly OrderDbContext _context;
        private readonly IMessageProducer _messageProducer;
        public OrderService(OrderDbContext context,IMessageProducer messageProducer)
        {
            _context = context;
            _messageProducer = messageProducer;
        }
        private async Task<Order> Save(OrderDto orderDto)
        {
            var order = new Order
            {
                ProductName = orderDto.ProductName,
                Quantity = orderDto.Quantity,
                Price = orderDto.Price
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            return order;
        }

        public async Task SaveOrder(OrderDto orderDto)
        {
            var order = await Save(orderDto);
            if (order is not null)
                _messageProducer.SendMessage(order,"orders",routingKey: "orders");
        }
    }
}