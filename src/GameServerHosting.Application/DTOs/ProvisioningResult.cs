namespace GameServerHosting.Application.DTOs;

/// <summary>
/// Result of a server provisioning operation
/// </summary>
public class ProvisioningResult
{
    /// <summary>
    /// Whether the provisioning was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// IP address of the provisioned server
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Port of the provisioned server
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Provider's unique identifier for the server
    /// </summary>
    public string? ProviderServerId { get; set; }

    /// <summary>
    /// Error message if provisioning failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Error code if provisioning failed
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Whether the failure is retryable
    /// </summary>
    public bool IsRetryable { get; set; }
}
