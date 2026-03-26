namespace WebApplication1.Domain;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id);
    Task<IEnumerable<Order>> GetPagedAsync(int page, int limit);
    Task AddAsync(Order order);
    Task UpdateAsync(Order order);
}