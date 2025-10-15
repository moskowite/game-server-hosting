namespace GameServerHosting.Shared.Events;

/// <summary>
/// Event published when a server health check fails
/// </summary>
public class ServerHealthCheckFailed : BaseEvent
{
    /// <summary>
    /// The ID of the game server that failed the health check
    /// </summary>
    public Guid GameServerId { get; set; }

    /// <summary>
    /// The status of the server
    /// </summary>
    public string ServerStatus { get; set; } = string.Empty;

    /// <summary>
    /// The IP address of the server
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// The port of the server
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Reason for the health check failure
    /// </summary>
    public string FailureReason { get; set; } = string.Empty;

    /// <summary>
    /// Number of consecutive failed checks
    /// </summary>
    public int ConsecutiveFailures { get; set; }

    /// <summary>
    /// When the health check was performed
    /// </summary>
    public DateTime CheckedAt { get; set; }
}
