namespace GameServerHosting.Application.Interfaces;

/// <summary>
/// Unit of Work pattern interface for managing repository transactions
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Repository for User entities
    /// </summary>
    IUserRepository Users { get; }

    /// <summary>
    /// Repository for ServerType entities
    /// </summary>
    IServerTypeRepository ServerTypes { get; }

    /// <summary>
    /// Repository for ServerPlan entities
    /// </summary>
    IServerPlanRepository ServerPlans { get; }

    /// <summary>
    /// Repository for GameServer entities
    /// </summary>
    IGameServerRepository GameServers { get; }

    /// <summary>
    /// Repository for Order entities
    /// </summary>
    IOrderRepository Orders { get; }

    /// <summary>
    /// Repository for Payment entities
    /// </summary>
    IPaymentRepository Payments { get; }

    /// <summary>
    /// Repository for CartItem entities
    /// </summary>
    ICartItemRepository CartItems { get; }

    /// <summary>
    /// Saves all changes made in this unit of work
    /// </summary>
    /// <returns>Number of state entries written to the database</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins a database transaction
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the current transaction
    /// </summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the current transaction
    /// </summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
