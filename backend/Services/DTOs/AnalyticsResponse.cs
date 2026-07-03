namespace backend.Services.DTOs;

public record AnalyticsResponse(
    decimal TotalRevenue,
    IEnumerable<OrderStatusCount> OrdersByStatus,
    IEnumerable<TopProduct> TopProducts
);