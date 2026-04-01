using Microsoft.EntityFrameworkCore;
using WebApplication1.Domain;

namespace WebApplication1.Data;

public class OrderRepository(AppDbContext context) : IOrderRepository
{
    public async Task<Order?> GetByIdAsync(Guid id) =>
        await context.Orders.FirstOrDefaultAsync(o => o.Id == id);

    public async Task<IEnumerable<Order>> GetPagedAsync(int page, int limit, Guid? userId = null, string? sortBy = null, bool isDescending = true)
    {
        var query = context.Orders.AsNoTracking();

        if (userId.HasValue)
            query = query.Where(o => o.UserId == userId.Value);

        query = sortBy?.ToLower() switch
        {
            "amount" => isDescending ? query.OrderByDescending(o => o.TotalAmount) : query.OrderBy(o => o.TotalAmount),
            "status" => isDescending ? query.OrderByDescending(o => o.Status) : query.OrderBy(o => o.Status),
            _ => isDescending ? query.OrderByDescending(o => o.CreatedAt) : query.OrderBy(o => o.CreatedAt)
        };

        return await query
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync();
    }

    public async Task AddAsync(Order order)
    {
        await context.Orders.AddAsync(order);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Order order)
    {
        context.Orders.Update(order);
        await context.SaveChangesAsync();
    }
}