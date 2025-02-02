using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using API.Models;
using Libs.Data.Context;
using Libs.Data.Models;
using Libs.Services;
using Libs.Repositories;
using Libs.Enums;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly FileSystemWatcherService _watcherService;
        private readonly OrderRepository _orderRepository;

        public OrderController(FileSystemWatcherService watcherService, OrderRepository orderRepository)
        {
            _watcherService = watcherService;
            _orderRepository = orderRepository;
        }

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitOrder(SubmitOrderRequest request)
        {
            try
            {
                var newOrder = new Order{
                    OrderId = request.OrderId,
                    Customer = request.Customer,
                    Rolls = request.Rolls,
                    Scanner = request.Scanner,
                };

                var resp = await _orderRepository.AddOrder(newOrder);
                // var id = await Task.Run(() => _watcherService.CreateWatcher(request.path));
                // return Ok(new { Id = id });
                return Ok(resp);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new SubmitOrderResponse{Message = ex.Message});
            }
        }

        [HttpPost("complete")]
        public async Task<IActionResult> ProcessOrder(CompleteOrderRequest req)
        {
            try
            {
                var resp = await _orderRepository.ProcessOrder(req.OrderId);
                return Ok(resp);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new CompleteOrderResponse{Message = ex.Message});
            }
        }

        [HttpDelete("cancel/{id}")]
        public async Task<IActionResult> CancelOrder(Guid id)
        {
            // await Task.Run(() => _watcherService.DisposeWatcher(id));
            return Ok($"Watcher disposed for ID {id}");
        }

        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders(string? search = null, OrderStatus? status = null)
        {
            var orders = await _orderRepository.GetOrders(search,status);
            return Ok(orders);
        }
    }
}