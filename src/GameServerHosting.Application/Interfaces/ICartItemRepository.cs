using GameServerHosting.Domain.Entities;

namespace GameServerHosting.Application.Interfaces;

/// <summary>
/// Repository interface for CartItem entity operations
/// </summary>
public interface ICartItemRepository : IRepository<CartItem>
{
    /// <summary>
    /// Gets cart items by user ID
    /// </summary>
    Task<IEnumerable<CartItem>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cart items with full details (includes server plan and server type)
    /// </summary>
    Task<IEnumerable<CartItem>> GetWithDetailsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a cart item by user ID and server plan ID
    /// </summary>
    Task<CartItem?> GetByUserAndPlanAsync(Guid userId, Guid serverPlanId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all cart items for a user
    /// </summary>
    Task ClearCartAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total cart value for a user
    /// </summary>
    Task<decimal> GetCartTotalAsync(Guid userId, CancellationToken cancellationToken = default);
}
