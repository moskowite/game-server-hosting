using GameServerHosting.Domain.Entities;
using GameServerHosting.Domain.Enums;

namespace GameServerHosting.Application.Interfaces;

/// <summary>
/// Repository interface for GameServer entity operations
/// </summary>
public interface IGameServerRepository : IRepository<GameServer>
{
    /// <summary>
    /// Gets game servers by user ID
    /// </summary>
    Task<IEnumerable<GameServer>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a game server with its server type
    /// </summary>
    Task<GameServer?> GetWithServerTypeAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets game servers by status
    /// </summary>
    Task<IEnumerable<GameServer>> GetByStatusAsync(ServerStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets expired game servers
    /// </summary>
    Task<IEnumerable<GameServer>> GetExpiredServersAsync(DateTime asOf, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets servers with stale heartbeats
    /// </summary>
    Task<IEnumerable<GameServer>> GetServersWithStaleHeartbeatsAsync(DateTime olderThan, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a server name is already used by a user
    /// </summary>
    Task<bool> IsServerNameUsedAsync(Guid userId, string serverName, Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a game server with full details (includes server type, orders, etc.)
    /// </summary>
    Task<GameServer?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
}
