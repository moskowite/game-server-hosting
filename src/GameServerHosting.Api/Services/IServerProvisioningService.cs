using GameServerHosting.Shared.Entities;

namespace GameServerHosting.Api.Services;

/// <summary>
/// Service interface for orchestrating game server provisioning
/// </summary>
public interface IServerProvisioningService
{
    /// <summary>
    /// Provisions a new game server based on an order
    /// </summary>
    /// <param name="orderId">The order ID to provision a server for</param>
    /// <returns>The created game server ID</returns>
    Task<Guid> ProvisionServerAsync(Guid orderId);

    /// <summary>
    /// Gets the current provisioning status of a server
    /// </summary>
    /// <param name="serverId">The server ID to check</param>
    /// <returns>The provisioning status information</returns>
    Task<ProvisioningStatus> GetProvisioningStatusAsync(Guid serverId);

    /// <summary>
    /// Rolls back a failed provisioning attempt
    /// </summary>
    /// <param name="serverId">The server ID to rollback</param>
    /// <param name="reason">The reason for rollback</param>
    /// <returns>True if rollback was successful</returns>
    Task<bool> RollbackProvisioningAsync(Guid serverId, string reason);

    /// <summary>
    /// Validates that a server is ready for provisioning
    /// </summary>
    /// <param name="orderId">The order ID to validate</param>
    /// <returns>True if the order is valid for provisioning</returns>
    Task<bool> ValidateProvisioningAsync(Guid orderId);
}

/// <summary>
/// Represents the current status of a provisioning operation
/// </summary>
public class ProvisioningStatus
{
    /// <summary>
    /// Server ID
    /// </summary>
    public Guid ServerId { get; set; }

    /// <summary>
    /// Current status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Progress percentage (0-100)
    /// </summary>
    public int Progress { get; set; }

    /// <summary>
    /// Current step being executed
    /// </summary>
    public string CurrentStep { get; set; } = string.Empty;

    /// <summary>
    /// Estimated time remaining in minutes
    /// </summary>
    public int? EstimatedTimeRemainingMinutes { get; set; }

    /// <summary>
    /// Error message if failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Provisioning start time
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Provisioning completion time
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// IP address once provisioned
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Port once provisioned
    /// </summary>
    public int? Port { get; set; }
}
