using GameServerHosting.Shared.Enums;

namespace GameServerHosting.Shared.Entities;

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
