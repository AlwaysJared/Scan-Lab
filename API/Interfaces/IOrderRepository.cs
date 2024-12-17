using Libs.Classes;
using Libs.Data.Models;

interface IOrderRepository : IDisposable
{
    Task<IEnumerable<Order>> GetOrders(string? search, OrderStatus? status);
    Order GetOrder(Guid id);
    Task<SystemResponse> AddOrder(Order order);
    Task<SystemResponse> UpdateOrder(Order order);
    Task<SystemResponse> ProcessOrder(string id);
    void Save();
}