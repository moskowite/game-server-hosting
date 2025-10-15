namespace GameServerHosting.Shared.Events;

/// <summary>
/// Event published when server provisioning fails
/// </summary>
public class ServerProvisioningFailed : BaseEvent
{
    /// <summary>
    /// The ID of the game server that failed to provision
    /// </summary>
    public Guid GameServerId { get; set; }

    /// <summary>
    /// The ID of the order associated with this provisioning attempt
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// Error message describing why provisioning failed
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Error code from the cloud provider (if applicable)
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Number of retry attempts made before failing
    /// </summary>
    public int RetryAttempts { get; set; }

    /// <summary>
    /// Whether this failure is retryable
    /// </summary>
    public bool IsRetryable { get; set; }
}
