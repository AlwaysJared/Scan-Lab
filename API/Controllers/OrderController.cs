using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using API.Models;
using Libs.Data.Context;
using Libs.Data.Models;
using Libs.Services;
using Libs.Repositories;
using Libs.Enums;
using API.Models.RequestsResponses;
using System.Text.Json;
using System.Text.Json.Serialization;

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
                var newOrder = new Order
                {
                    OrderId = request.OrderId,
                    Customer = request.Customer,
                    CustomerInitials = request.CustomerInitials,
                    Rolls = request.Rolls,
                    Scanner = request.Scanner,
                };

                var resp = await _orderRepository.AddOrder(newOrder);
                // var id = await Task.Run(() => _watcherService.CreateWatcher(request.path));
                // return Ok(new { Id = id });
                if (!resp.IsSuccess)
                    return BadRequest(resp.Message);

                return Ok(resp);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new SubmitOrderResponse { Message = ex.Message });
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
                return BadRequest(new CompleteOrderResponse { Message = ex.Message });
            }
        }

        [HttpDelete("cancel/{id}")]
        public async Task<IActionResult> CancelOrder(string id)
        {
            // await Task.Run(() => _watcherService.DisposeWatcher(id));
            // return Ok($"Watcher disposed for ID {id}");
            throw new NotImplementedException();
        }

        [HttpPost("delete")]
        public async Task<IActionResult> DeleteOrder(DeleteOrderRequest req)
        {
            try
            {
                var resp = await _orderRepository.DeleteOrder(req.OrderId);

                if (!resp.IsSuccess)
                    return BadRequest(resp.Message);

                return Ok("Order successfully deleted");
            }
            catch (ArgumentException ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("orders")]
        public async Task<IActionResult> GetOrders(GetOrdersRequest req)
        {
            try
            {
                var orders = await _orderRepository.GetOrders(req.search, req.orderStatus, req.scannerId);
                var options = new JsonSerializerOptions
                {
                    ReferenceHandler = ReferenceHandler.Preserve,
                    WriteIndented = true
                };

                string json = JsonSerializer.Serialize(orders, options);

                return Ok(json);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }

        }
    }
}