using GameServerHosting.Domain.Entities;
using GameServerHosting.Domain.Enums;

namespace GameServerHosting.Application.Interfaces;

/// <summary>
/// Repository interface for ServerPlan entity operations
/// </summary>
public interface IServerPlanRepository : IRepository<ServerPlan>
{
    /// <summary>
    /// Gets all active server plans
    /// </summary>
    Task<IEnumerable<ServerPlan>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets server plans by server type ID
    /// </summary>
    Task<IEnumerable<ServerPlan>> GetByServerTypeIdAsync(Guid serverTypeId, bool? isActive = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a server plan with its server type
    /// </summary>
    Task<ServerPlan?> GetWithServerTypeAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets server plans by billing cycle
    /// </summary>
    Task<IEnumerable<ServerPlan>> GetByBillingCycleAsync(BillingCycle billingCycle, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a plan with the same name exists for a server type
    /// </summary>
    Task<bool> ExistsWithNameAsync(Guid serverTypeId, string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
}
