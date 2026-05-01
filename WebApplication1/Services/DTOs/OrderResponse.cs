namespace WebApplication1.Services.DTOs;
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public record OrderResponse(Guid Id, string Status, string Products, string ShippingAddress, decimal TotalAmount, DateTime CreatedAt);