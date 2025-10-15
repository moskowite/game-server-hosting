using GameServerHosting.Application.Interfaces;
using GameServerHosting.Domain.Entities;
using GameServerHosting.Domain.Enums;
using GameServerHosting.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GameServerHosting.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for ServerPlan entity operations
/// </summary>
public class ServerPlanRepository : Repository<ServerPlan>, IServerPlanRepository
{
    public ServerPlanRepository(GameServerDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ServerPlan>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(sp => sp.IsActive)
            .OrderBy(sp => sp.DisplayOrder)
            .ThenBy(sp => sp.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ServerPlan>> GetByServerTypeIdAsync(Guid serverTypeId, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(sp => sp.ServerTypeId == serverTypeId);

        if (isActive.HasValue)
        {
            query = query.Where(sp => sp.IsActive == isActive.Value);
        }

        return await query
            .OrderBy(sp => sp.DisplayOrder)
            .ThenBy(sp => sp.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ServerPlan?> GetWithServerTypeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(sp => sp.ServerType)
            .FirstOrDefaultAsync(sp => sp.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ServerPlan>> GetByBillingCycleAsync(BillingCycle billingCycle, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(sp => sp.BillingCycle == billingCycle)
            .OrderBy(sp => sp.DisplayOrder)
            .ThenBy(sp => sp.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsWithNameAsync(Guid serverTypeId, string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(sp => sp.ServerTypeId == serverTypeId && sp.Name.ToLower() == name.ToLower());

        if (excludeId.HasValue)
        {
            query = query.Where(sp => sp.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
