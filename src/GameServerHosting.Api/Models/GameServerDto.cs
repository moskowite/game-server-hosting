using GameServerHosting.Shared.Enums;

namespace GameServerHosting.Api.Models;

/// <summary>
/// DTO for game server information
/// </summary>
public class GameServerDto
{
    public Guid Id { get; set; }
    public string ServerName { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public int? Port { get; set; }
    public ServerStatus Status { get; set; }
    public string Version { get; set; } = string.Empty;
    public DateTime? DeployedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastHeartbeatAt { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Server Type Information
    public string ServerTypeName { get; set; } = string.Empty;
    public string GameName { get; set; } = string.Empty;
    public int CpuCores { get; set; }
    public int RamMb { get; set; }
    public int MaxPlayers { get; set; }
}
