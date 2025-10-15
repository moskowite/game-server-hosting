using GameServerHosting.Shared.Enums;

namespace GameServerHosting.Api.Services;

/// <summary>
/// Service interface for managing game server lifecycle and status
/// </summary>
public interface IServerManagementService
{
    /// <summary>
    /// Starts a stopped or suspended server
    /// </summary>
    /// <param name="serverId">The server ID to start</param>
    /// <returns>True if successful</returns>
    Task<bool> StartServerAsync(Guid serverId);

    /// <summary>
    /// Stops a running server
    /// </summary>
    /// <param name="serverId">The server ID to stop</param>
    /// <returns>True if successful</returns>
    Task<bool> StopServerAsync(Guid serverId);

    /// <summary>
    /// Restarts a running server
    /// </summary>
    /// <param name="serverId">The server ID to restart</param>
    /// <returns>True if successful</returns>
    Task<bool> RestartServerAsync(Guid serverId);

    /// <summary>
    /// Deletes a server permanently
    /// </summary>
    /// <param name="serverId">The server ID to delete</param>
    /// <returns>True if successful</returns>
    Task<bool> DeleteServerAsync(Guid serverId);

    /// <summary>
    /// Updates the status of a server
    /// </summary>
    /// <param name="serverId">The server ID</param>
    /// <param name="newStatus">The new status</param>
    /// <param name="reason">Optional reason for the status change</param>
    /// <returns>True if successful</returns>
    Task<bool> UpdateServerStatusAsync(Guid serverId, ServerStatus newStatus, string? reason = null);

    /// <summary>
    /// Gets the current health status of a server
    /// </summary>
    /// <param name="serverId">The server ID to check</param>
    /// <returns>Server health information</returns>
    Task<ServerHealthStatus> CheckServerHealthAsync(Guid serverId);

    /// <summary>
    /// Gets server metrics (CPU, RAM, etc.)
    /// </summary>
    /// <param name="serverId">The server ID</param>
    /// <returns>Server metrics</returns>
    Task<ServerMetrics> GetServerMetricsAsync(Guid serverId);
}

/// <summary>
/// Health status information for a server
/// </summary>
public class ServerHealthStatus
{
    /// <summary>
    /// Server ID
    /// </summary>
    public Guid ServerId { get; set; }

    /// <summary>
    /// Whether the server is healthy
    /// </summary>
    public bool IsHealthy { get; set; }

    /// <summary>
    /// Current server status
    /// </summary>
    public ServerStatus Status { get; set; }

    /// <summary>
    /// Last successful health check time
    /// </summary>
    public DateTime? LastHealthCheckAt { get; set; }

    /// <summary>
    /// Last successful heartbeat time
    /// </summary>
    public DateTime? LastHeartbeatAt { get; set; }

    /// <summary>
    /// Cloud provider status
    /// </summary>
    public string? ProviderStatus { get; set; }

    /// <summary>
    /// Whether the server is reachable
    /// </summary>
    public bool IsReachable { get; set; }

    /// <summary>
    /// Response time in milliseconds
    /// </summary>
    public int? ResponseTimeMs { get; set; }

    /// <summary>
    /// Health check error message if unhealthy
    /// </summary>
    public string? ErrorMessage { get; set; }
}
