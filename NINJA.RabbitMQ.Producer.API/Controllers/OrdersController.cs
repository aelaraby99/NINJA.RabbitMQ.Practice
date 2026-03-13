using Microsoft.AspNetCore.Mvc;
using NINJA.RabbitMQ.Producer.API.DTOs;
using NINJA.RabbitMQ.Producer.API.Services;
namespace NINJA.RabbitMQ.Producer.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OrdersController: ControllerBase
    {
        private readonly IOrderService _orders;

        public OrdersController(IOrderService orders)
        {
            _orders = orders;
        }
        [HttpPost]
        public async Task<IActionResult> Orders(OrderDto orderDto)
        {
            await _orders.SaveOrder(orderDto);
            return Ok();
        }
    }
}