using GameServerHosting.Application.Interfaces;
using GameServerHosting.Domain.Entities;
using GameServerHosting.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GameServerHosting.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for CartItem entity operations
/// </summary>
public class CartItemRepository : Repository<CartItem>, ICartItemRepository
{
    public CartItemRepository(GameServerDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<CartItem>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(ci => ci.UserId == userId)
            .OrderBy(ci => ci.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<CartItem>> GetWithDetailsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(ci => ci.UserId == userId)
            .Include(ci => ci.ServerPlan)
                .ThenInclude(sp => sp.ServerType)
            .OrderBy(ci => ci.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<CartItem?> GetByUserAndPlanAsync(Guid userId, Guid serverPlanId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(ci => ci.UserId == userId && ci.ServerPlanId == serverPlanId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task ClearCartAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var cartItems = await _dbSet
            .Where(ci => ci.UserId == userId)
            .ToListAsync(cancellationToken);

        _dbSet.RemoveRange(cartItems);
    }

    /// <inheritdoc/>
    public async Task<decimal> GetCartTotalAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(ci => ci.UserId == userId)
            .SumAsync(ci => ci.Price * ci.Quantity, cancellationToken);
    }
}
