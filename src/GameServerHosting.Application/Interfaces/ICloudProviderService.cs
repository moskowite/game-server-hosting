namespace GameServerHosting.Application.Interfaces;

/// <summary>
/// Interface for cloud provider operations (DigitalOcean, AWS, Azure, etc.)
/// </summary>
public interface ICloudProviderService
{
    /// <summary>
    /// Creates a new server instance on the cloud provider
    /// </summary>
    /// <param name="request">Server provision request details</param>
    /// <returns>The provision result with server details</returns>
    Task<ProvisionResult> CreateServerAsync(ServerProvisionRequest request);

    /// <summary>
    /// Gets the current status of a server
    /// </summary>
    /// <param name="providerId">The cloud provider's server ID</param>
    /// <returns>Server status information</returns>
    Task<ServerInfo> GetServerStatusAsync(string providerId);

    /// <summary>
    /// Starts a stopped server
    /// </summary>
    /// <param name="providerId">The cloud provider's server ID</param>
    /// <returns>True if successful</returns>
    Task<bool> StartServerAsync(string providerId);

    /// <summary>
    /// Stops a running server
    /// </summary>
    /// <param name="providerId">The cloud provider's server ID</param>
    /// <returns>True if successful</returns>
    Task<bool> StopServerAsync(string providerId);

    /// <summary>
    /// Restarts a server
    /// </summary>
    /// <param name="providerId">The cloud provider's server ID</param>
    /// <returns>True if successful</returns>
    Task<bool> RestartServerAsync(string providerId);

    /// <summary>
    /// Deletes a server permanently
    /// </summary>
    /// <param name="providerId">The cloud provider's server ID</param>
    /// <returns>True if successful</returns>
    Task<bool> DeleteServerAsync(string providerId);

    /// <summary>
    /// Gets server resource metrics (CPU, memory, etc.)
    /// </summary>
    /// <param name="providerId">The cloud provider's server ID</param>
    /// <returns>Server metrics</returns>
    Task<ServerMetrics> GetServerMetricsAsync(string providerId);
}

/// <summary>
/// Request for provisioning a new server
/// </summary>
public class ServerProvisionRequest
{
    /// <summary>
    /// Unique server ID
    /// </summary>
    public Guid ServerId { get; set; }

    /// <summary>
    /// User ID who owns the server
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Server plan ID
    /// </summary>
    public Guid ServerPlanId { get; set; }

    /// <summary>
    /// Server type (game type)
    /// </summary>
    public string ServerType { get; set; } = string.Empty;

    /// <summary>
    /// Server name
    /// </summary>
    public string ServerName { get; set; } = string.Empty;

    /// <summary>
    /// Preferred region/datacenter
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// RAM in MB
    /// </summary>
    public int RamMb { get; set; }

    /// <summary>
    /// CPU cores
    /// </summary>
    public int CpuCores { get; set; }

    /// <summary>
    /// Storage in GB
    /// </summary>
    public int StorageGb { get; set; }

    /// <summary>
    /// Server-specific configuration
    /// </summary>
    public Dictionary<string, string> Configuration { get; set; } = new();

    /// <summary>
    /// Game version to install
    /// </summary>
    public string? GameVersion { get; set; }
}

/// <summary>
/// Result of a provisioning operation
/// </summary>
public class ProvisionResult
{
    /// <summary>
    /// Whether the provisioning was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Cloud provider's server ID
    /// </summary>
    public string? ProviderId { get; set; }

    /// <summary>
    /// Assigned IP address
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Assigned port
    /// </summary>
    public int? Port { get; set; }

    /// <summary>
    /// Provisioning completion timestamp
    /// </summary>
    public DateTime? ProvisionedAt { get; set; }

    /// <summary>
    /// Error message if failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Information about a server's current state
/// </summary>
public class ServerInfo
{
    /// <summary>
    /// Cloud provider's server ID
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Current status (active, off, new, archive)
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Whether the server is healthy
    /// </summary>
    public bool IsHealthy { get; set; }

    /// <summary>
    /// IP address
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// Region/datacenter
    /// </summary>
    public string? Region { get; set; }
}

/// <summary>
/// Server resource usage metrics
/// </summary>
public class ServerMetrics
{
    /// <summary>
    /// CPU usage percentage (0-100)
    /// </summary>
    public double CpuUsagePercent { get; set; }

    /// <summary>
    /// Memory usage percentage (0-100)
    /// </summary>
    public double MemoryUsagePercent { get; set; }

    /// <summary>
    /// Disk usage percentage (0-100)
    /// </summary>
    public double DiskUsagePercent { get; set; }

    /// <summary>
    /// Network inbound bytes per second
    /// </summary>
    public long NetworkInBytesPerSec { get; set; }

    /// <summary>
    /// Network outbound bytes per second
    /// </summary>
    public long NetworkOutBytesPerSec { get; set; }

    /// <summary>
    /// Timestamp of metrics
    /// </summary>
    public DateTime Timestamp { get; set; }
}
