using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Services.DTOs;

// [x] TODO record vs class or structure + избегание алокацийx
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