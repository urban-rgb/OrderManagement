using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using WebApplication1.Domain;
using WebApplication1.Services;
using WebApplication1.Services.DTOs;
// ... остальные using

public class OrderService(IOrderRepository repository, IDistributedCache cache) : IOrderService
{
    public async Task<OrderResponse> GetOrderAsync(Guid id)
    {
        string cacheKey = $"order:{id}";

        // Try get from cache
        var cachedOrder = await cache.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cachedOrder))
        {
            return JsonSerializer.Deserialize<OrderResponse>(cachedOrder)!;
        }

        // If object != in cache -> check DB
        var order = await repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundDomainException("Order not found");

        var response = MapToResponse(order);

        // Save cache (30min)
        await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(response), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
        });

        return response;
    }

    public async Task UpdateAddressAsync(Guid id, string newAddress)
    {
        var order = await repository.GetByIdAsync(id) ?? throw new KeyNotFoundDomainException("Order not found");

        if (order.Status is OrderStatus.InTransit or OrderStatus.Delivered)
            throw new ConflictDomainException("Cannot change address");

        order.ShippingAddress = newAddress;
        await repository.UpdateAsync(order);

        // Deleting old cache
        await cache.RemoveAsync($"order:{id}");
    }

    public async Task CancelOrderAsync(Guid id)
    {
        var order = await repository.GetByIdAsync(id) ?? throw new KeyNotFoundDomainException("Order not found");
        if (order.Status == OrderStatus.Delivered)
            throw new DomainException("Cannot cancel delivered order");

        order.Status = OrderStatus.Cancelled;
        await repository.UpdateAsync(order);

        // Cache invalidation
        await cache.RemoveAsync($"order:{id}");
    }

    public async Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            Status = OrderStatus.Pending,
            Products = request.Products,
            ShippingAddress = request.ShippingAddress,
            TotalAmount = request.TotalAmount,
            CreatedAt = DateTime.UtcNow
        };
        await repository.AddAsync(order);
        return MapToResponse(order);
    }

    public async Task<IEnumerable<OrderResponse>> GetOrdersAsync(int page, int limit)
    {
        var orders = await repository.GetPagedAsync(page, limit);
        return orders.Select(MapToResponse);
    }

    private static OrderResponse MapToResponse(Order o) =>
        new(o.Id, o.Status.ToString(), o.Products, o.ShippingAddress, o.TotalAmount, o.CreatedAt);
}