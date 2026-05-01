using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Data;
using OrderManagement.Domain;
using OrderManagement.Services;
using OrderManagement.Services.DTOs;

public class OrderService(
    AppDbContext context,
    ICacheService cache,
    ILogger<OrderService> logger,
    TimeProvider timeProvider,
    IMapper mapper) : IOrderService
{
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
            return Result<OrderResponse>.Failure($"Service failure: {ex.Message}", ErrorType.Failure);
        }
    }

    public async Task<Result<bool>> UpdateAddressAsync(Guid id, UpdateOrderAddressRequest request)
    {
        try
        {
            var order = await context.Orders.FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return Result<bool>.Failure("Order not found", ErrorType.NotFound);

            if (order.Status is OrderStatus.InTransit or OrderStatus.Delivered or OrderStatus.Cancelled)
                return Result<bool>.Failure("Cannot change address", ErrorType.Conflict);

            order.ShippingAddress = request.NewAddress;
            await context.SaveChangesAsync();
            await ClearCacheAsync(id, order.UserId);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating address");
            return Result<bool>.Failure($"Internal error: {ex.Message}", ErrorType.Failure);
        }
    }

    public async Task<Result<bool>> CancelOrderAsync(Guid id)
    {
        try
        {
            var order = await context.Orders.FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return Result<bool>.Failure("Order not found", ErrorType.NotFound);

            if (order.Status is OrderStatus.InTransit or OrderStatus.Delivered or OrderStatus.Cancelled)
                return Result<bool>.Failure("Cannot cancel delivered order", ErrorType.Conflict);

            order.Status = OrderStatus.Cancelled;
            await context.SaveChangesAsync();
            await ClearCacheAsync(id, order.UserId);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error cancelling order");
            return Result<bool>.Failure($"Internal error: {ex.Message}", ErrorType.Failure);
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
            await InvalidateListCacheAsync(order.UserId);

            return Result<OrderResponse>.Success(mapper.Map<OrderResponse>(order));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating order");
            return Result<OrderResponse>.Failure($"Internal error: {ex.Message}", ErrorType.Failure);
        }
    }

    public async Task<Result<IEnumerable<OrderResponse>>> GetOrdersAsync(int page, int limit, Guid? userId = null, string? sortBy = null, bool isDescending = true)
    {
        try
        {
            string versionKey = userId.HasValue ? $"orders:version:{userId}" : "orders:version:all";
            var version = await cache.GetStringAsync(versionKey) ?? "1";

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
            return Result<IEnumerable<OrderResponse>>.Failure($"Database error: {ex.Message}", ErrorType.Failure);
        }
    }

    private async Task ClearCacheAsync(Guid id, Guid userId)
    {
        try
        {
            await cache.RemoveAsync($"order:{id}");
            await InvalidateListCacheAsync(userId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to clear cache for order {Id}", id);
        }
    }

    private async Task InvalidateListCacheAsync(Guid userId)
    {
        try
        {
            await IncrementVersionAsync($"orders:version:{userId}");
            await IncrementVersionAsync("orders:version:all");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to invalidate list cache");
        }
    }

    private async Task IncrementVersionAsync(string key)
    {
        var version = await cache.GetStringAsync(key) ?? "1";
        if (int.TryParse(version, out int v))
            await cache.SetRawAsync(key, (v + 1).ToString());
        else
            await cache.SetRawAsync(key, "1");
    }
}