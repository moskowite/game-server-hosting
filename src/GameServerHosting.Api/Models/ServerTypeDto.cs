namespace GameServerHosting.Api.Models;

/// <summary>
/// Data transfer object for ServerType
/// </summary>
public class ServerTypeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string GameName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int CpuCores { get; set; }
    public int RamMb { get; set; }
    public int DiskGb { get; set; }
    public int MaxPlayers { get; set; }
    public decimal HourlyPrice { get; set; }
    public decimal MonthlyPrice { get; set; }
    public string DockerImage { get; set; } = string.Empty;
    public string DefaultConfig { get; set; } = "{}";
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
