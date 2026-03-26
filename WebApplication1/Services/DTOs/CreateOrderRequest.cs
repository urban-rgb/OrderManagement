using System.ComponentModel.DataAnnotations;
namespace WebApplication1.Services.DTOs;

public record CreateOrderRequest([Required] string Products,
    [Required] string ShippingAddress,
    [Range(0.01, double.MaxValue, ErrorMessage ="Sum has to be more than 0")] decimal TotalAmount);