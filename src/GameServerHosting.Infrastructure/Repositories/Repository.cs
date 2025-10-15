using GameServerHosting.Application.Interfaces;
using GameServerHosting.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace GameServerHosting.Infrastructure.Repositories;

/// <summary>
/// Generic repository implementation for common data access operations
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly GameServerDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(GameServerDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = context.Set<T>();
    }

    /// <inheritdoc/>
    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<IEnumerable<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(predicate).ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<T?> FirstOrDefaultAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(predicate, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<bool> AnyAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(predicate, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<int> CountAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.CountAsync(predicate, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
        return entity;
    }

    /// <inheritdoc/>
    public virtual async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddRangeAsync(entities, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual void Update(T entity)
    {
        _dbSet.Update(entity);
    }

    /// <inheritdoc/>
    public virtual void UpdateRange(IEnumerable<T> entities)
    {
        _dbSet.UpdateRange(entities);
    }

    /// <inheritdoc/>
    public virtual void Remove(T entity)
    {
        _dbSet.Remove(entity);
    }

    /// <inheritdoc/>
    public virtual void RemoveRange(IEnumerable<T> entities)
    {
        _dbSet.RemoveRange(entities);
    }

    /// <inheritdoc/>
    public virtual IQueryable<T> AsQueryable()
    {
        return _dbSet.AsQueryable();
    }
}
