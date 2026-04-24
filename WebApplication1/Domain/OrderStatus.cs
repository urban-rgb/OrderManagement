namespace WebApplication1.Domain;

/// <summary>
/// Represents the lifecycle stages of an order. 
/// Numeric values are used for threshold comparisons.
/// </summary>
public enum OrderStatus
{
    Pending = 100,
    Paid = 200,
    InTransit = 300,
    Delivered = 400,
    Cancelled = 500,

    AddressChangeLimit = InTransit,
    CancellationLimit = Delivered
}