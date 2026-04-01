using WebApplication1.Services.DTOs;

namespace WebApplication1.Services;

public interface IOrderService
{
    Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request);
    Task<OrderResponse> GetOrderAsync(Guid id);
    Task<IEnumerable<OrderResponse>> GetOrdersAsync(int page, int limit, Guid? userId = null, string? sortBy = null, bool isDescending = true);
    Task UpdateAddressAsync(Guid id, string newAddress);
    Task CancelOrderAsync(Guid id);
}