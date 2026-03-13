using NINJA.RabbitMQ.Producer.API.DTOs;
namespace NINJA.RabbitMQ.Producer.API.Services
{
    public interface IOrderService
    {
        public Task SaveOrder(OrderDto orderDto);
    }
}