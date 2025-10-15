using GameServerHosting.Domain.Entities;
using GameServerHosting.Domain.Enums;

namespace GameServerHosting.Application.Interfaces;

/// <summary>
/// Repository interface for Payment entity operations
/// </summary>
public interface IPaymentRepository : IRepository<Payment>
{
    /// <summary>
    /// Gets payments by user ID
    /// </summary>
    Task<IEnumerable<Payment>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets payments by order ID
    /// </summary>
    Task<IEnumerable<Payment>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a payment by transaction ID
    /// </summary>
    Task<Payment?> GetByTransactionIdAsync(string transactionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a payment with related entities (order, user)
    /// </summary>
    Task<Payment?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets payments by status
    /// </summary>
    Task<IEnumerable<Payment>> GetByStatusAsync(PaymentStatus status, CancellationToken cancellationToken = default);
}
