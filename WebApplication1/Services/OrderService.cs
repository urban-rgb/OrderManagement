using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Domain;
using WebApplication1.Services;
using WebApplication1.Services.DTOs;

public class OrderService(
    AppDbContext context,
    ICacheService cache,
    ILogger<OrderService> logger,
    TimeProvider timeProvider,
    IMapper mapper) : IOrderService
{
    private const string ListVersionKey = "orders:list:version";
    private static readonly TimeSpan OrderCacheExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan ListCacheExpiration = TimeSpan.FromMinutes(10);

    public async Task<Result<OrderResponse>> GetOrderAsync(Guid id)
    {
        try
        {
            string cacheKey = $"order:{id}";
            var cachedOrder = await cache.GetAsync<OrderResponse>(cacheKey);
            if (cachedOrder != null) return Result<OrderResponse>.Success(cachedOrder);

            var order = await context.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return Result<OrderResponse>.Failure("Order not found", ErrorType.NotFound);

            var response = mapper.Map<OrderResponse>(order);
            await cache.SetAsync(cacheKey, response, OrderCacheExpiration);
            return Result<OrderResponse>.Success(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetOrderAsync for {Id}", id);
            return Result<OrderResponse>.Failure("Service failure", ErrorType.Failure);
        }
    }

    public async Task<Result<bool>> UpdateAddressAsync(Guid id, UpdateOrderAddressRequest request)
    {
        try
        {
            var order = await context.Orders.FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return Result<bool>.Failure("Order not found", ErrorType.NotFound);

            if (order.Status >= OrderStatus.AddressChangeLimit)
                return Result<bool>.Failure("Cannot change address", ErrorType.Conflict);

            order.ShippingAddress = request.NewAddress;
            await context.SaveChangesAsync();
            await ClearCacheAsync(id);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating address");
            return Result<bool>.Failure("Internal error", ErrorType.Failure);
        }
    }

    public async Task<Result<bool>> CancelOrderAsync(Guid id)
    {
        try
        {
            var order = await context.Orders.FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return Result<bool>.Failure("Order not found", ErrorType.NotFound);

            if (order.Status >= OrderStatus.CancellationLimit)
                return Result<bool>.Failure("Cannot cancel delivered order", ErrorType.Conflict);

            order.Status = OrderStatus.Cancelled;
            await context.SaveChangesAsync();
            await ClearCacheAsync(id);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error cancelling order");
            return Result<bool>.Failure("Internal error", ErrorType.Failure);
        }
    }

    public async Task<Result<OrderResponse>> CreateOrderAsync(CreateOrderRequest request)
    {
        try
        {
            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                Status = OrderStatus.Pending,
                Products = request.Products,
                ShippingAddress = request.ShippingAddress,
                TotalAmount = request.TotalAmount,
                CreatedAt = timeProvider.GetUtcNow().UtcDateTime
            };

            context.Orders.Add(order);
            await context.SaveChangesAsync();
            await InvalidateListCacheAsync();

            return Result<OrderResponse>.Success(mapper.Map<OrderResponse>(order));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating order");
            return Result<OrderResponse>.Failure("Internal error", ErrorType.Failure);
        }
    }

    public async Task<Result<IEnumerable<OrderResponse>>> GetOrdersAsync(int page, int limit, Guid? userId = null, string? sortBy = null, bool isDescending = true)
    {
        try
        {
            var version = await cache.GetStringAsync(ListVersionKey) ?? "1";
            var cacheKey = $"orders:list:{version}:{userId}:{page}:{limit}:{sortBy}:{isDescending}";

            var cachedData = await cache.GetAsync<IEnumerable<OrderResponse>>(cacheKey);
            if (cachedData != null) return Result<IEnumerable<OrderResponse>>.Success(cachedData);

            var query = context.Orders.AsNoTracking();
            if (userId.HasValue) query = query.Where(o => o.UserId == userId.Value);

            query = sortBy?.ToLower() switch
            {
                "amount" => isDescending ? query.OrderByDescending(o => o.TotalAmount) : query.OrderBy(o => o.TotalAmount),
                "status" => isDescending ? query.OrderByDescending(o => o.Status) : query.OrderBy(o => o.Status),
                _ => isDescending ? query.OrderByDescending(o => o.CreatedAt) : query.OrderBy(o => o.CreatedAt)
            };

            var entities = await query.Skip((page - 1) * limit).Take(limit).ToListAsync();
            var response = mapper.Map<IEnumerable<OrderResponse>>(entities);

            await cache.SetAsync(cacheKey, response, ListCacheExpiration);
            return Result<IEnumerable<OrderResponse>>.Success(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching orders");
            return Result<IEnumerable<OrderResponse>>.Failure("Database error", ErrorType.Failure);
        }
    }

    private async Task ClearCacheAsync(Guid id)
    {
        try
        {
            await cache.RemoveAsync($"order:{id}");
            await InvalidateListCacheAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to clear cache for order {Id}", id);
        }
    }

    private async Task InvalidateListCacheAsync()
    {
        try
        {
            var version = await cache.GetStringAsync(ListVersionKey) ?? "1";
            if (int.TryParse(version, out int v))
                await cache.SetRawAsync(ListVersionKey, (v + 1).ToString());
            else
                await cache.SetRawAsync(ListVersionKey, "1");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to invalidate list cache");
        }
    }
}