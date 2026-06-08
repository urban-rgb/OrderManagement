namespace backend.Services.DTOs;

public record OrderItemResponse(Guid Id, string Name, int Quantity, decimal UnitPrice);
