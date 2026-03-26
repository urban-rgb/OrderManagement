using Microsoft.EntityFrameworkCore;
using WebApplication1.Domain;

namespace WebApplication1.Data;

public class OrderRepository(AppDbContext context) : IOrderRepository
{
    //READ
    public async Task<Order?> GetByIdAsync(Guid id) =>
        await context.Orders.FirstOrDefaultAsync(o => o.Id == id);

    //Basic Queries
    public async Task<IEnumerable<Order>> GetPagedAsync(int page, int limit) =>
        await context.Orders
            .AsNoTracking()
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync();

    //CREATE
    public async Task AddAsync(Order order)
    {
        await context.Orders.AddAsync(order);
        await context.SaveChangesAsync();
    }

    //UPDATE
    public async Task UpdateAsync(Order order)
    {
        context.Orders.Update(order);
        await context.SaveChangesAsync();
    }
}