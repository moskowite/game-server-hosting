using System.Linq.Expressions;

namespace GameServerHosting.Application.Interfaces;

/// <summary>
/// Generic repository interface for common data access operations
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>
    /// Gets an entity by its ID
    /// </summary>
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all entities
    /// </summary>
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds entities based on a predicate
    /// </summary>
    Task<IEnumerable<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the first entity matching the predicate, or null
    /// </summary>
    Task<T?> FirstOrDefaultAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if any entity matches the predicate
    /// </summary>
    Task<bool> AnyAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts entities matching the predicate
    /// </summary>
    Task<int> CountAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new entity
    /// </summary>
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple entities
    /// </summary>
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing entity
    /// </summary>
    void Update(T entity);

    /// <summary>
    /// Updates multiple entities
    /// </summary>
    void UpdateRange(IEnumerable<T> entities);

    /// <summary>
    /// Removes an entity
    /// </summary>
    void Remove(T entity);

    /// <summary>
    /// Removes multiple entities
    /// </summary>
    void RemoveRange(IEnumerable<T> entities);

    /// <summary>
    /// Gets queryable for advanced scenarios (use with caution)
    /// </summary>
    IQueryable<T> AsQueryable();
}
