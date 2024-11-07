using Libs.Classes;
using Libs.Data.Models;

interface IOrderRepository : IDisposable
{
    Task<IEnumerable<Order>> GetOrders();
    Order GetOrder(Guid id);
    Task<SystemResponse> AddOrder(Order order);
    Task<SystemResponse> UpdateOrder(Order order);
    void Save();
}