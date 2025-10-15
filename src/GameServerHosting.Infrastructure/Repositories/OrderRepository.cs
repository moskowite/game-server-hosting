using GameServerHosting.Application.Interfaces;
using GameServerHosting.Domain.Entities;
using GameServerHosting.Domain.Enums;
using GameServerHosting.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GameServerHosting.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Order entity operations
/// </summary>
public class OrderRepository : Repository<Order>, IOrderRepository
{
    public OrderRepository(GameServerDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Order>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(o => o.UserId == userId)
            .Include(o => o.GameServer)
            .Include(o => o.ServerPlan)
                .ThenInclude(sp => sp!.ServerType)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Order?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(o => o.GameServer)
            .Include(o => o.ServerPlan)
                .ThenInclude(sp => sp!.ServerType)
            .Include(o => o.User)
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Order>> GetByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(o => o.Status == status)
            .Include(o => o.User)
            .Include(o => o.ServerPlan)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(o => o.GameServer)
            .Include(o => o.ServerPlan)
                .ThenInclude(sp => sp!.ServerType)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> GetTodayOrderCountAsync(string datePrefix, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(o => o.OrderNumber.StartsWith($"ORD-{datePrefix}"))
            .CountAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Order>> GetPendingOrdersByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(o => o.UserId == userId && o.Status == OrderStatus.Pending)
            .Include(o => o.ServerPlan)
                .ThenInclude(sp => sp!.ServerType)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Order?> GetByPaymentTransactionIdAsync(string transactionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(o => o.ServerPlan)
                .ThenInclude(sp => sp!.ServerType)
            .FirstOrDefaultAsync(o => o.PaymentTransactionId == transactionId, cancellationToken);
    }
}
