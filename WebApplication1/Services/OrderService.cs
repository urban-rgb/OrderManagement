using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using WebApplication1.Domain;
using WebApplication1.Services;
using WebApplication1.Services.DTOs;

public class OrderService(IOrderRepository repository, IDistributedCache cache, ILogger<OrderService> logger) : IOrderService
{
    private const string ListVersionKey = "orders:list:version";

    public async Task<OrderResponse> GetOrderAsync(Guid id)
    {
        string cacheKey = $"order:{id}";
        var cachedOrder = await cache.GetStringAsync(cacheKey);

        if (!string.IsNullOrEmpty(cachedOrder))
            return JsonSerializer.Deserialize<OrderResponse>(cachedOrder)!;

        var order = await repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundDomainException("Order not found");

        var response = MapToResponse(order);
        await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(response), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
        });

        return response;
    }

    public async Task UpdateAddressAsync(Guid id, string newAddress)
    {
        if (string.IsNullOrWhiteSpace(newAddress))
            throw new DomainException("Address cannot be empty");

        var order = await repository.GetByIdAsync(id) ?? throw new KeyNotFoundDomainException("Order not found");

        if (order.Status is OrderStatus.InTransit or OrderStatus.Delivered)
            throw new ConflictDomainException("Cannot change address");

        order.ShippingAddress = newAddress;
        await repository.UpdateAsync(order);

        await cache.RemoveAsync($"order:{id}");
        await InvalidateListCacheAsync();

        logger.LogInformation("Address for order {Id} updated", id);
    }

    public async Task CancelOrderAsync(Guid id)
    {
        var order = await repository.GetByIdAsync(id) ?? throw new KeyNotFoundDomainException("Order not found");
        if (order.Status == OrderStatus.Delivered)
            throw new DomainException("Cannot cancel delivered order");

        order.Status = OrderStatus.Cancelled;
        await repository.UpdateAsync(order);

        await cache.RemoveAsync($"order:{id}");
        await InvalidateListCacheAsync();

        logger.LogInformation("Order {Id} cancelled", id);
    }

    public async Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request)
    {
        if (request.TotalAmount <= 0)
            throw new DomainException("Total amount must be greater than zero");

        if (string.IsNullOrWhiteSpace(request.Products))
            throw new DomainException("Products list cannot be empty");

        if (string.IsNullOrWhiteSpace(request.ShippingAddress))
            throw new DomainException("Shipping address cannot be empty");

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
        await repository.AddAsync(order);

        await InvalidateListCacheAsync();

        logger.LogInformation("Order {Id} created for user {UserId}", order.Id, order.UserId);
        return MapToResponse(order);
    }

    public async Task<IEnumerable<OrderResponse>> GetOrdersAsync(int page, int limit, Guid? userId = null, string? sortBy = null, bool isDescending = true)
    {
        var version = await cache.GetStringAsync(ListVersionKey) ?? "1";
        string cacheKey = $"orders:list:{version}:{userId}:{page}:{limit}:{sortBy}:{isDescending}";

        var cachedData = await cache.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cachedData))
            return JsonSerializer.Deserialize<IEnumerable<OrderResponse>>(cachedData)!;

        var orders = await repository.GetPagedAsync(page, limit, userId, sortBy, isDescending);
        var response = orders.Select(MapToResponse).ToList();

        await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(response), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        });

        return response;
    }

    private async Task InvalidateListCacheAsync()
    {
        var version = await cache.GetStringAsync(ListVersionKey) ?? "1";
        if (int.TryParse(version, out int v))
        {
            await cache.SetStringAsync(ListVersionKey, (v + 1).ToString());
        }
    }

    private static OrderResponse MapToResponse(Order o) =>
        new(o.Id, o.Status.ToString(), o.Products, o.ShippingAddress, o.TotalAmount, o.CreatedAt);
}