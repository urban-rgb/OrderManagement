using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using WebApplication1.Data;
using WebApplication1.Domain;
using WebApplication1.Services;
using WebApplication1.Services.DTOs;

public class OrderService(AppDbContext context, IDistributedCache cache, ILogger<OrderService> logger) : IOrderService
{
    private const string ListVersionKey = "orders:list:version";

    public async Task<Result<OrderResponse>> GetOrderAsync(Guid id) 
    {
        string cacheKey = $"order:{id}";

        try
        {
            var cachedOrder = await cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedOrder))
            {
                var data = JsonSerializer.Deserialize<OrderResponse>(cachedOrder)!;
                return Result<OrderResponse>.Success(data);
            }
        }
        catch (Exception ex) {
            logger.LogWarning(ex, "Redis is unavailable while getting order {id}", id);
        }
        

        var response = await context.Orders
            .AsNoTracking()
            .Where(o => o.Id == id)
            .Select(o => new OrderResponse(o.Id, o.Status.ToString(), o.Products, o.ShippingAddress, o.TotalAmount, o.CreatedAt))
            .FirstOrDefaultAsync();

        if (response == null)
        {
            return Result<OrderResponse>.Failure("Order not found", ErrorType.NotFound);
        }

        try
        {
            await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(response), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
            });
        }
        catch (Exception ex) {
            logger.LogWarning(ex, "Failed to set cache for order {Id}", id);
        }

        return Result<OrderResponse>.Success(response);

    }

    // [x] TODO result pattern - в этом и других местах лучше использовать его, чем мой способ, попробовать заменить
    // [x]? TODO обезопасить вылет
    public async Task<Result<bool>> UpdateAddressAsync(Guid id, UpdateOrderAddressRequest request)
    {
        if (request == null)
            return Result<bool>.Failure("Invalid address data", ErrorType.Validation);

        try
        {
            var order = await context.Orders.FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return Result<bool>.Failure("Order not found", ErrorType.NotFound);

            int currentStatus = (int)order.Status;

            /*if (order.Status is OrderStatus.InTransit or OrderStatus.Delivered)*/
            if (currentStatus >= 300)
                return Result<bool>.Failure("Cannot change address", ErrorType.Conflict);

            order.ShippingAddress = request.NewAddress;

            await context.SaveChangesAsync();

            await ClearCacheAsync(id);

            logger.LogInformation("Address for order {Id} updated", id);
            return Result<bool>.Success(true);
        }
        catch (DbUpdateConcurrencyException)
        {
            logger.LogWarning("Concurrency conflict during address update for order {Id}", id);
            return Result<bool>.Failure("Data was modified by another user. Please refresh.", ErrorType.Conflict);
        }
        catch(Exception ex)
        {
            logger.LogError(ex, "Error updating address for order {Id}", id);
            return Result<bool>.Failure("Database error occurred", ErrorType.Failure);
        }
    }

    public async Task<Result<bool>> CancelOrderAsync(Guid id)
    {
        try
        {
            var order = await context.Orders.FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return Result<bool>.Failure("Order not found", ErrorType.NotFound);

            int currentStatus = (int)order.Status;
            /*vif (order.Status == OrderStatus.Delivered)*/
            if (currentStatus >= 400)
                return Result<bool>.Failure("Cannot cancel delivered order", ErrorType.Conflict);

            order.Status = OrderStatus.Cancelled;
            await context.SaveChangesAsync();

            // [x] ещё раз просмотреть этот метод и попробовать объеденить с RemoveAsync
            //await cache.RemoveAsync($"order:{id}");
            //await InvalidateListCacheAsync();
            await ClearCacheAsync(id);

            logger.LogInformation("Order {Id} cancelled", id);
            return Result<bool>.Success(true);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<bool>.Failure("Concurrency conflict. Order status was changed by another process.", ErrorType.Conflict);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error cancelling order {Id}", id);
            return Result<bool>.Failure("An internal error occurred", ErrorType.Failure);
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
                CreatedAt = DateTime.UtcNow
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            await InvalidateListCacheAsync();

            logger.LogInformation("Order {Id} created for user {UserId}", order.Id, order.UserId);
            return Result<OrderResponse>.Success(MapToResponse(order));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create order");
            return Result<OrderResponse>.Failure("Error saving order to database", ErrorType.Failure);
        }
    }

    public async Task<Result<IEnumerable<OrderResponse>>> GetOrdersAsync(int page, int limit, Guid? userId = null, string? sortBy = null, bool isDescending = true)
    {
        string version = "1";
        try
        {
            version = await cache.GetStringAsync(ListVersionKey) ?? "1";
        }
        catch {  }

        string cacheKey = $"orders:list:{version}:{userId}:{page}:{limit}:{sortBy}:{isDescending}";

        try
        {
            var cachedData = await cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                var data = JsonSerializer.Deserialize<IEnumerable<OrderResponse>>(cachedData)!;
                return Result<IEnumerable<OrderResponse>>.Success(data);
            }
        }
        catch {  }

        var query = context.Orders.AsNoTracking();

        if (userId.HasValue)
            query = query.Where(o => o.UserId == userId.Value);

        query = sortBy?.ToLower() switch
        {
            "amount" => isDescending ? query.OrderByDescending(o => o.TotalAmount) : query.OrderBy(o => o.TotalAmount),
            "status" => isDescending ? query.OrderByDescending(o => o.Status) : query.OrderBy(o => o.Status),
            _ => isDescending ? query.OrderByDescending(o => o.CreatedAt) : query.OrderBy(o => o.CreatedAt)
        };


        var response = await query
            .Skip((page - 1) * limit) // пагинация
            .Take(limit)              // пагинация
            .Select(o => new OrderResponse(o.Id, o.Status.ToString(), o.Products, o.ShippingAddress, o.TotalAmount, o.CreatedAt)) // SELECT из SQL
            .ToListAsync(); // отправка запроса в бд

        try
        {
            await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(response), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            });
        }
        catch {  }

        return Result<IEnumerable<OrderResponse>>.Success(response);
    }

    private async Task ClearCacheAsync(Guid id)
    {
        try
        {
            await cache.RemoveAsync($"order:{id}");
            await InvalidateListCacheAsync();
        }
        catch(Exception ex)
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
            {
                await cache.SetStringAsync(ListVersionKey, (v + 1).ToString());
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to invalidate list cache version");
        }
    }

    private static OrderResponse MapToResponse(Order o) =>
        new(o.Id, o.Status.ToString(), o.Products, o.ShippingAddress, o.TotalAmount, o.CreatedAt);
}