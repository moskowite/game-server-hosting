using GameServerHosting.Domain.Entities;
using GameServerHosting.Domain.Enums;

namespace GameServerHosting.Application.Interfaces;

/// <summary>
/// Repository interface for Order entity operations
/// </summary>
public interface IOrderRepository : IRepository<Order>
{
    /// <summary>
    /// Gets orders by user ID
    /// </summary>
    Task<IEnumerable<Order>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an order with full details (includes server, plan, payments, user)
    /// </summary>
    Task<Order?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets orders by status
    /// </summary>
    Task<IEnumerable<Order>> GetByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an order by order number
    /// </summary>
    Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets orders created today with a specific date prefix
    /// </summary>
    Task<int> GetTodayOrderCountAsync(string datePrefix, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending orders for a user
    /// </summary>
    Task<IEnumerable<Order>> GetPendingOrdersByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an order by payment transaction ID
    /// </summary>
    Task<Order?> GetByPaymentTransactionIdAsync(string transactionId, CancellationToken cancellationToken = default);
}
