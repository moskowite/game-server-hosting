using GameServerHosting.Domain.Entities;

namespace GameServerHosting.Application.Interfaces;

/// <summary>
/// Repository interface for ServerType entity operations
/// </summary>
public interface IServerTypeRepository : IRepository<ServerType>
{
    /// <summary>
    /// Gets a server type by name
    /// </summary>
    Task<ServerType?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active server types
    /// </summary>
    Task<IEnumerable<ServerType>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets server types by game name
    /// </summary>
    Task<IEnumerable<ServerType>> GetByGameNameAsync(string gameName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a server type with its plans
    /// </summary>
    Task<ServerType?> GetWithPlansAsync(Guid id, CancellationToken cancellationToken = default);
}
