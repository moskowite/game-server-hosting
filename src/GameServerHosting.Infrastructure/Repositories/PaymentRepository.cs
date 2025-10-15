using GameServerHosting.Application.Interfaces;
using GameServerHosting.Domain.Entities;
using GameServerHosting.Domain.Enums;
using GameServerHosting.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GameServerHosting.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Payment entity operations
/// </summary>
public class PaymentRepository : Repository<Payment>, IPaymentRepository
{
    public PaymentRepository(GameServerDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Payment>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.UserId == userId)
            .Include(p => p.Order)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Payment>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.OrderId == orderId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Payment?> GetByTransactionIdAsync(string transactionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Order)
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.TransactionId == transactionId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Payment?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Order)
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Payment>> GetByStatusAsync(PaymentStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.Status == status)
            .Include(p => p.Order)
            .Include(p => p.User)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
