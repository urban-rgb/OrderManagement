using backend.Domain;
using backend.Services.DTOs;

namespace backend.Services;


public interface IOrderService
{
    Task<Result<OrderResponse>> CreateOrderAsync(CreateOrderRequest request, Guid userId);
    Task<Result<OrderResponse>> GetOrderAsync(Guid id);
    Task<Result<IEnumerable<OrderResponse>>> GetOrdersAsync(int page, int limit, Guid? userId = null, string? sortBy = null, bool isDescending = true, OrderStatus? status = null, DateTime? dateFrom = null, DateTime? dateTo = null);
    Task<Result<bool>> UpdateAddressAsync(Guid id, UpdateOrderAddressRequest request);
    Task<Result<bool>> CancelOrderAsync(Guid id);

    Task<Result<bool>> ForceUpdateStatusAsync(Guid id, OrderStatus newStatus);

    Task<Result<AnalyticsResponse>> GetAnalyticsAsync();
}