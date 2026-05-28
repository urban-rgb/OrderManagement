namespace backend.Services.DTOs;

public record OrderResponse(Guid Id, Guid UserId, string Status, string Products, string ShippingAddress, decimal TotalAmount, DateTime CreatedAt);