using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace WebApplication1.Services.DTOs;

[ExcludeFromCodeCoverage]
public record CreateOrderRequest(
    [Required]
    Guid UserId,

    [Required]
    string Products,

    [Required]
    string ShippingAddress,

    [Range(0.01, double.MaxValue)]
    decimal TotalAmount
);