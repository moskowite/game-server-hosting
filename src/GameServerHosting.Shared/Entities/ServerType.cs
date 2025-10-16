namespace GameServerHosting.Shared.Entities;

/// <summary>
/// Represents a type of game server available for hosting
/// </summary>
public class ServerType
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Server type name (e.g., "Minecraft Basic")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Game name (e.g., "Minecraft", "CS:GO")
    /// </summary>
    public string GameName { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the server type
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Number of CPU cores
    /// </summary>
    public int CpuCores { get; set; }

    /// <summary>
    /// RAM in megabytes
    /// </summary>
    public int RamMb { get; set; }

    /// <summary>
    /// Disk space in gigabytes
    /// </summary>
    public int DiskGb { get; set; }

    /// <summary>
    /// Maximum concurrent players
    /// </summary>
    public int MaxPlayers { get; set; }

    /// <summary>
    /// Price per hour in USD
    /// </summary>
    public decimal HourlyPrice { get; set; }

    /// <summary>
    /// Price per month in USD
    /// </summary>
    public decimal MonthlyPrice { get; set; }

    /// <summary>
    /// Docker image reference
    /// </summary>
    public string DockerImage { get; set; } = string.Empty;

    /// <summary>
    /// Default server configuration (JSON)
    /// </summary>
    public string DefaultConfig { get; set; } = "{}";

    /// <summary>
    /// Default game/software version
    /// </summary>
    public string? DefaultVersion { get; set; }

    /// <summary>
    /// Whether this server type is available for new orders
    /// </summary>
    public bool IsActive { get; set; } = true;

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
    /// Game servers using this server type
    /// </summary>
    public ICollection<GameServer> GameServers { get; set; } = new List<GameServer>();

    /// <summary>
    /// Billing plans available for this server type
    /// </summary>
    public ICollection<ServerPlan> ServerPlans { get; set; } = new List<ServerPlan>();
}
