using GameServerHosting.Application.Interfaces;
using GameServerHosting.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace GameServerHosting.Infrastructure.Repositories;

/// <summary>
/// Unit of Work implementation for managing repository transactions
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly GameServerDbContext _context;
    private IDbContextTransaction? _currentTransaction;

    private IUserRepository? _users;
    private IServerTypeRepository? _serverTypes;
    private IServerPlanRepository? _serverPlans;
    private IGameServerRepository? _gameServers;
    private IOrderRepository? _orders;
    private IPaymentRepository? _payments;
    private ICartItemRepository? _cartItems;
    private IAsyncOperationRepository? _asyncOperations;

    public UnitOfWork(GameServerDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc/>
    public IUserRepository Users =>
        _users ??= new UserRepository(_context);

    /// <inheritdoc/>
    public IServerTypeRepository ServerTypes =>
        _serverTypes ??= new ServerTypeRepository(_context);

    /// <inheritdoc/>
    public IServerPlanRepository ServerPlans =>
        _serverPlans ??= new ServerPlanRepository(_context);

    /// <inheritdoc/>
    public IGameServerRepository GameServers =>
        _gameServers ??= new GameServerRepository(_context);

    /// <inheritdoc/>
    public IOrderRepository Orders =>
        _orders ??= new OrderRepository(_context);

    /// <inheritdoc/>
    public IPaymentRepository Payments =>
        _payments ??= new PaymentRepository(_context);

    /// <inheritdoc/>
    public ICartItemRepository CartItems =>
        _cartItems ??= new CartItemRepository(_context);

    /// <inheritdoc/>
    public IAsyncOperationRepository AsyncOperations =>
        _asyncOperations ??= new AsyncOperationRepository(_context);

    /// <inheritdoc/>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
        {
            throw new InvalidOperationException("A transaction is already in progress.");
        }

        _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
        {
            throw new InvalidOperationException("No transaction is in progress.");
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await _currentTransaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }
    }

    /// <inheritdoc/>
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
        {
            throw new InvalidOperationException("No transaction is in progress.");
        }

        try
        {
            await _currentTransaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _currentTransaction?.Dispose();
        _context.Dispose();
    }
}
