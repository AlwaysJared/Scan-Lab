using Libs.Classes;
using Libs.Data.Context;
using Libs.Data.Models;
using Microsoft.EntityFrameworkCore;

public class OrderRepository : IOrderRepository, IDisposable
{
    private ScanLabContext context;
    public OrderRepository(ScanLabContext context)
    {
        this.context = context;
    }
    public async Task<SystemResponse> AddOrder(Order order)
    {
        var res = context.Orders.Add(order);
        await context.SaveChangesAsync();
        return new SystemResponse(){
            IsSuccess = true,
        };
    }

    public void Dispose()
    {
        context.Dispose();
    }

    public Order GetOrder(Guid id)
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<Order>> GetOrders(string? search, OrderStatus? status)
    {
        var orders = await context.Orders.ToListAsync();
        if(!String.IsNullOrWhiteSpace(search) || status.HasValue)
        {
            
        }
        return orders;
    }

    public void Save()
    {
        throw new NotImplementedException();
    }

    public Task<SystemResponse> UpdateOrder(Order order)
    {
        throw new NotImplementedException();
    }
}