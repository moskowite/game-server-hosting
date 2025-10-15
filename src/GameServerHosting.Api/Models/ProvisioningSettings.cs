namespace GameServerHosting.Api.Models;

/// <summary>
/// Configuration settings for DigitalOcean cloud provider
/// </summary>
public class DigitalOceanSettings
{
    /// <summary>
    /// DigitalOcean API token
    /// </summary>
    public string ApiToken { get; set; } = string.Empty;

    /// <summary>
    /// API base URL
    /// </summary>
    public string ApiBaseUrl { get; set; } = "https://api.digitalocean.com/v2";

    /// <summary>
    /// Default region for server provisioning
    /// </summary>
    public string DefaultRegion { get; set; } = "nyc3";

    /// <summary>
    /// SSH key ID for server access
    /// </summary>
    public string? SshKeyId { get; set; }

    /// <summary>
    /// Timeout for provisioning operations in seconds
    /// </summary>
    public int ProvisioningTimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Enable monitoring on droplets
    /// </summary>
    public bool EnableMonitoring { get; set; } = true;

    /// <summary>
    /// Enable backups on droplets
    /// </summary>
    public bool EnableBackups { get; set; } = false;

    /// <summary>
    /// Default tags to apply to droplets
    /// </summary>
    public List<string> DefaultTags { get; set; } = new() { "game-server" };
}

/// <summary>
/// General provisioning configuration
/// </summary>
public class ProvisioningSettings
{
    /// <summary>
    /// Maximum number of concurrent provisioning operations
    /// </summary>
    public int MaxConcurrentProvisions { get; set; } = 10;

    /// <summary>
    /// Number of retry attempts on failure
    /// </summary>
    public int RetryAttempts { get; set; } = 3;

    /// <summary>
    /// Delay between retry attempts in seconds
    /// </summary>
    public List<int> RetryDelaySeconds { get; set; } = new() { 60, 300, 900 };

    /// <summary>
    /// Health check interval in minutes
    /// </summary>
    public int HealthCheckIntervalMinutes { get; set; } = 5;

    /// <summary>
    /// Automatically rollback on failure
    /// </summary>
    public bool AutoRollbackOnFailure { get; set; } = true;

    /// <summary>
    /// Send email notifications
    /// </summary>
    public bool SendNotifications { get; set; } = true;

    /// <summary>
    /// Default cloud provider
    /// </summary>
    public string DefaultProvider { get; set; } = "DigitalOcean";
}

/// <summary>
/// Cloud providers configuration
/// </summary>
public class CloudProvidersSettings
{
    /// <summary>
    /// Default cloud provider to use
    /// </summary>
    public string Default { get; set; } = "DigitalOcean";

    /// <summary>
    /// DigitalOcean settings
    /// </summary>
    public DigitalOceanSettings DigitalOcean { get; set; } = new();
}
