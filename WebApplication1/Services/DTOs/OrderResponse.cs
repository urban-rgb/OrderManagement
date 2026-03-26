namespace WebApplication1.Services.DTOs;

public record OrderResponse(Guid Id, string Status, string Products, string ShippingAddress, decimal TotalAmount, DateTime CreatedAt);