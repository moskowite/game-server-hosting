using GameServerHosting.Domain.Enums;

namespace GameServerHosting.Client.Models;

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
