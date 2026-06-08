using backend.Domain;

namespace backend.Services.DTOs;

public record ForceUpdateStatusRequest(OrderStatus NewStatus);
