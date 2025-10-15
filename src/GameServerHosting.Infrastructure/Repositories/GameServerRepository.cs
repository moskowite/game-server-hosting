using GameServerHosting.Application.Interfaces;
using GameServerHosting.Domain.Entities;
using GameServerHosting.Domain.Enums;
using GameServerHosting.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GameServerHosting.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for GameServer entity operations
/// </summary>
public class GameServerRepository : Repository<GameServer>, IGameServerRepository
{
    public GameServerRepository(GameServerDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<GameServer>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(gs => gs.UserId == userId)
            .Include(gs => gs.ServerType)
            .OrderByDescending(gs => gs.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<GameServer?> GetWithServerTypeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(gs => gs.ServerType)
            .FirstOrDefaultAsync(gs => gs.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<GameServer>> GetByStatusAsync(ServerStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(gs => gs.Status == status)
            .Include(gs => gs.ServerType)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<GameServer>> GetExpiredServersAsync(DateTime asOf, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(gs => gs.ExpiresAt.HasValue && gs.ExpiresAt.Value <= asOf)
            .Include(gs => gs.ServerType)
            .Include(gs => gs.User)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<GameServer>> GetServersWithStaleHeartbeatsAsync(DateTime olderThan, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(gs => gs.LastHeartbeatAt.HasValue && gs.LastHeartbeatAt.Value < olderThan)
            .Include(gs => gs.ServerType)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> IsServerNameUsedAsync(Guid userId, string serverName, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(gs => gs.UserId == userId && gs.ServerName == serverName);

        if (excludeId.HasValue)
        {
            query = query.Where(gs => gs.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<GameServer?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(gs => gs.ServerType)
            .Include(gs => gs.User)
            .Include(gs => gs.Orders)
            .FirstOrDefaultAsync(gs => gs.Id == id, cancellationToken);
    }
}
