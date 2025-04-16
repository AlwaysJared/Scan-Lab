using Libs.Classes;
using Libs.Data.Models;
using Libs.Enums;

namespace Libs.Interfaces
{
    public interface IOrderRepository : IDisposable
    {
        Task<IEnumerable<Order>> GetOrders(string? search, OrderStatus? status, Guid? scannerId);
        Task<Order?> GetOrder(string orderId);
        Task<SystemResponse> AddOrder(Order order);
        Task<SystemResponse> UpdateOrder(Order order);
        Task<SystemResponse> ProcessOrder(string id);
        Task<SystemResponse> DeleteOrder(string orderId);
    }
}