using GameServerHosting.Domain.Entities;

namespace GameServerHosting.Application.Interfaces;

/// <summary>
/// Repository interface for AsyncOperation entities
/// </summary>
public interface IAsyncOperationRepository : IRepository<AsyncOperation>
{
    /// <summary>
    /// Gets an async operation by its correlation ID
    /// </summary>
    /// <param name="correlationId">The correlation ID</param>
    /// <returns>The async operation if found, null otherwise</returns>
    Task<AsyncOperation?> GetByCorrelationIdAsync(Guid correlationId);

    /// <summary>
    /// Gets all async operations for a specific user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>List of async operations</returns>
    Task<IEnumerable<AsyncOperation>> GetByUserIdAsync(Guid userId);

    /// <summary>
    /// Gets async operations by status
    /// </summary>
    /// <param name="status">The operation status</param>
    /// <returns>List of async operations</returns>
    Task<IEnumerable<AsyncOperation>> GetByStatusAsync(Domain.Enums.AsyncOperationStatus status);
}
