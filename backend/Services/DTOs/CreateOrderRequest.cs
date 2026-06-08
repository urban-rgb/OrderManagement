using System.ComponentModel.DataAnnotations;

namespace backend.Services.DTOs;

public record CreateOrderRequest(
    [Required, MinLength(1)]
    IEnumerable<OrderItemRequest> Items,

    [Required]
    string ShippingAddress
);