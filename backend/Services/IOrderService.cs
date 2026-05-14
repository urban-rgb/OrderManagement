using backend.Domain;
using backend.Services.DTOs;

namespace backend.Services;


public interface IOrderService
{
    /// <summary>
    /// Creates a new order in the system based on the provided request.
    /// </summary>
    /// <param name="request">Data required to create the order.</param>
    /// <returns>The details of the newly created order.</returns>
    Task<Result<OrderResponse>> CreateOrderAsync(CreateOrderRequest request);

    /// <summary>
    /// Retrieves a specific order by its unique identifier.
    /// </summary>
    /// <param name="id">The unique ID of the order.</param>
    /// <returns>The order details.</returns>
    /// <exception cref="KeyNotFoundDomainException">Thrown if the order does not exist.</exception>
    Task<Result<OrderResponse>> GetOrderAsync(Guid id);

    /// <summary>
    /// Retrieves a paginated list of orders, optionally filtered by user and sorted.
    /// </summary>
    /// <param name="page">Current page number (starting from 1).</param>
    /// <param name="limit">Number of items per page.</param>
    /// <param name="userId">Optional filter for a specific user.</param>
    /// <param name="sortBy">Field name to sort by (e.g., "amount", "status").</param>
    /// <param name="isDescending">Order direction.</param>
    /// <returns>A collection of order responses.</returns>
    Task<Result<IEnumerable<OrderResponse>>> GetOrdersAsync(int page, int limit, Guid? userId = null, string? sortBy = null, bool isDescending = true);

    /// <summary>
    /// Updates the delivery address for an existing order.
    /// </summary>
    /// <param name="id">The unique ID of the order.</param>
    /// <param name="newAddress">The new shipping address.</param>
    /// <exception cref="ConflictDomainException">Thrown if the order status prevents address changes.</exception>
    Task<Result<bool>> UpdateAddressAsync(Guid id, UpdateOrderAddressRequest request);

    /// <summary>
    /// Cancels an existing order.
    /// </summary>
    /// <param name="id">The unique ID of the order.</param>
    /// <exception cref="DomainException">Thrown if the order is already delivered.</exception>
    Task<Result<bool>> CancelOrderAsync(Guid id);
}