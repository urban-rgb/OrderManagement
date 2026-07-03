namespace backend.Services.DTOs;

public record OrderResponse(Guid Id, Guid UserId, string Status, IEnumerable<OrderItemResponse> Items, string ShippingAddress, decimal TotalAmount, DateTime CreatedAt);