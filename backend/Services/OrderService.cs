using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Domain;
using backend.Services;
using backend.Services.DTOs;

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

            var order = await context.Orders.AsNoTracking().Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
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
            await ClearCacheAsync(id, order.UserId);
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
            await ClearCacheAsync(id, order.UserId);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error cancelling order");
            return Result<bool>.Failure("Internal error", ErrorType.Failure);
        }
    }

    public async Task<Result<OrderResponse>> CreateOrderAsync(CreateOrderRequest request, Guid userId)
    {
        try
        {
            var items = request.Items.Select(i => new OrderItem
            {
                Id = Guid.NewGuid(),
                Name = i.Name,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList();

            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Status = OrderStatus.Pending,
                ShippingAddress = request.ShippingAddress,
                Items = items,
                TotalAmount = items.Sum(i => i.Quantity * i.UnitPrice),
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
            return Result<OrderResponse>.Failure("Internal error", ErrorType.Failure);
        }
    }

    public async Task<Result<IEnumerable<OrderResponse>>> GetOrdersAsync(int page, int limit, Guid? userId = null, string? sortBy = null, bool isDescending = true, OrderStatus? status = null, DateTime? dateFrom = null, DateTime? dateTo = null)
    {
        try
        {
            string versionKey = userId.HasValue ? $"orders:version:{userId}" : "orders:version:all";
            var version = await cache.GetStringAsync(versionKey) ?? "1";

            var cacheKey = $"orders:list:{version}:{userId}:{page}:{limit}:{sortBy}:{isDescending}:{(int?)status}:{dateFrom?.Date:yyyyMMdd}:{dateTo?.Date:yyyyMMdd}";

            var cachedData = await cache.GetAsync<IEnumerable<OrderResponse>>(cacheKey);
            if (cachedData != null) return Result<IEnumerable<OrderResponse>>.Success(cachedData);

            var query = context.Orders.AsNoTracking();
            if (userId.HasValue) query = query.Where(o => o.UserId == userId.Value);
            if (status.HasValue) query = query.Where(o => o.Status == status.Value);
            if (dateFrom.HasValue) query = query.Where(o => o.CreatedAt >= dateFrom.Value);
            if (dateTo.HasValue) query = query.Where(o => o.CreatedAt < dateTo.Value.AddDays(1));

            query = sortBy?.ToLower() switch
            {
                "amount" => isDescending ? query.OrderByDescending(o => o.TotalAmount) : query.OrderBy(o => o.TotalAmount),
                "status" => isDescending ? query.OrderByDescending(o => o.Status) : query.OrderBy(o => o.Status),
                _ => isDescending ? query.OrderByDescending(o => o.CreatedAt) : query.OrderBy(o => o.CreatedAt)
            };

            var entities = await query.Skip((page - 1) * limit).Take(limit).ToListAsync();

            if (entities.Count > 0)
            {
                var ids = entities.Select(o => o.Id).ToList();
                var items = await context.OrderItems.AsNoTracking()
                    .Where(i => ids.Contains(i.OrderId))
                    .ToListAsync();
                foreach (var order in entities)
                    order.Items = items.Where(i => i.OrderId == order.Id).ToList();
            }
            IEnumerable<OrderResponse> response = mapper.Map<List<OrderResponse>>(entities);

            await cache.SetAsync(cacheKey, response, ListCacheExpiration);
            return Result<IEnumerable<OrderResponse>>.Success(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching orders");
            return Result<IEnumerable<OrderResponse>>.Failure("Database error", ErrorType.Failure);
        }
    }

    public async Task<Result<bool>> ForceUpdateStatusAsync(Guid id, OrderStatus newStatus)
    {
        try
        {
            var order = await context.Orders.FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return Result<bool>.Failure("Order not found", ErrorType.NotFound);

            order.Status = newStatus;
            await context.SaveChangesAsync();
            await ClearCacheAsync(id, order.UserId);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error force-updating status for order {Id}", id);
            return Result<bool>.Failure("Internal error", ErrorType.Failure);
        }
    }

    public async Task<Result<AnalyticsResponse>> GetAnalyticsAsync()
    {
        try
        {
            var totalRevenue = await context.Orders.SumAsync(o => o.TotalAmount);

            var statusGroups = await context.Orders
                .AsNoTracking()
                .GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var countByStatus = statusGroups.ToDictionary(x => x.Status, x => x.Count);
            var ordersByStatus = Enum.GetValues<OrderStatus>()
                .Select(s => new OrderStatusCount(s.ToString(), countByStatus.GetValueOrDefault(s, 0)))
                .ToList();

            var topProducts = await context.OrderItems
                .AsNoTracking()
                .GroupBy(i => i.Name)
                .Select(g => new TopProduct(g.Key, g.Sum(i => i.Quantity)))
                .OrderByDescending(p => p.TotalQuantity)
                .Take(10)
                .ToListAsync();

            return Result<AnalyticsResponse>.Success(new AnalyticsResponse(totalRevenue, ordersByStatus, topProducts));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching analytics");
            return Result<AnalyticsResponse>.Failure("Database error", ErrorType.Failure);
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
