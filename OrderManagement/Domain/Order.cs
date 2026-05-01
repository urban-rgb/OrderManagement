using System.Diagnostics.CodeAnalysis;

namespace OrderManagement.Domain;

[ExcludeFromCodeCoverage]
public class Order
{
    /// <summary>
    /// Unique identifier for the Order.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The unique identifier of the user who placed the order.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Current lifecycle status of the order (e.g., Pending, Paid, Cancelled).
    /// </summary>
    public OrderStatus Status { get; set; }

    /// <summary>
    /// A string representation or list of products included in the order.
    /// </summary>
    public string Products { get; set; } = string.Empty;

    /// <summary>
    /// The physical address where the order should be delivered.
    /// </summary>
    public string ShippingAddress { get; set; } = string.Empty;

    /// <summary>
    /// Total monetary value of the order.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Timestamp when the order was initially created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Version number used for optimistic concurrency control.
    /// </summary>
    public uint Version { get; set; }
}