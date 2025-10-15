using GameServerHosting.Domain.Enums;

namespace GameServerHosting.Domain.Entities;

/// <summary>
/// Represents an individual game server instance
/// </summary>
public class GameServer
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Owner user ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Server type ID
    /// </summary>
    public Guid ServerTypeId { get; set; }

    /// <summary>
    /// Custom server name
    /// </summary>
    public string ServerName { get; set; } = string.Empty;

    /// <summary>
    /// Assigned IP address (null until provisioned)
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Assigned port number (null until provisioned)
    /// </summary>
    public int? Port { get; set; }

    /// <summary>
    /// Current server status
    /// </summary>
    public ServerStatus Status { get; set; } = ServerStatus.Pending;

    /// <summary>
    /// Server-specific configuration (JSON)
    /// </summary>
    public string Configuration { get; set; } = "{}";

    /// <summary>
    /// Game/software version
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Deployment completion timestamp
    /// </summary>
    public DateTime? DeployedAt { get; set; }

    /// <summary>
    /// Subscription expiration timestamp
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Last health check timestamp
    /// </summary>
    public DateTime? LastHeartbeatAt { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // Provisioning fields
    
    /// <summary>
    /// Cloud provider type (e.g., DigitalOcean, AWS, Azure)
    /// </summary>
    public string? ProviderType { get; set; }
    
    /// <summary>
    /// Cloud provider's server identifier (e.g., Droplet ID)
    /// </summary>
    public string? ProviderId { get; set; }
    
    /// <summary>
    /// Cloud provider region/datacenter
    /// </summary>
    public string? ProviderRegion { get; set; }
    
    /// <summary>
    /// When provisioning started
    /// </summary>
    public DateTime? ProvisioningStartedAt { get; set; }
    
    /// <summary>
    /// When provisioning completed
    /// </summary>
    public DateTime? ProvisioningCompletedAt { get; set; }
    
    /// <summary>
    /// Error message if provisioning failed
    /// </summary>
    public string? ProvisioningError { get; set; }
    
    /// <summary>
    /// SSH private key for server management (encrypted)
    /// </summary>
    public string? SshKeyPrivate { get; set; }
    
    /// <summary>
    /// SSH public key fingerprint
    /// </summary>
    public string? SshKeyPublic { get; set; }
    
    /// <summary>
    /// Health check status
    /// </summary>
    public string? HealthCheckStatus { get; set; }

    // Navigation properties
    
    /// <summary>
    /// Owner of this server
    /// </summary>
    public ApplicationUser User { get; set; } = null!;

    /// <summary>
    /// Server type definition
    /// </summary>
    public ServerType ServerType { get; set; } = null!;

    /// <summary>
    /// Orders related to this server
    /// </summary>
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
