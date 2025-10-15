using GameServerHosting.Application.Interfaces;
using GameServerHosting.Domain.Entities;
using GameServerHosting.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GameServerHosting.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for ServerType entity operations
/// </summary>
public class ServerTypeRepository : Repository<ServerType>, IServerTypeRepository
{
    public ServerTypeRepository(GameServerDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<ServerType?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(st => st.Name == name, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ServerType>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(st => st.IsActive)
            .OrderBy(st => st.GameName)
            .ThenBy(st => st.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ServerType>> GetByGameNameAsync(string gameName, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(st => st.GameName == gameName)
            .OrderBy(st => st.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ServerType?> GetWithPlansAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(st => st.ServerPlans)
            .FirstOrDefaultAsync(st => st.Id == id, cancellationToken);
    }
}
