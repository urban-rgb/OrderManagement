using System.ComponentModel.DataAnnotations;

namespace backend.Services.DTOs;

public record OrderItemRequest(
    [Required] string Name,
    [Range(1, int.MaxValue)] int Quantity,
    [Range(0.01, double.MaxValue)] decimal UnitPrice
);
