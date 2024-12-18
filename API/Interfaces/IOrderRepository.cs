using API.Models;
using Libs.Classes;
using Libs.Data.Models;

interface IOrderRepository : IDisposable
{
    Task<IEnumerable<Order>> GetOrders(string? search, OrderStatus? status);
    Order GetOrder(Guid id);
    Task<SystemResponse> AddOrder(SubmitOrderRequest order);
    Task<SystemResponse> UpdateOrder(Order order);
    Task<SystemResponse> ProcessOrder(string id);
    void Save();
}