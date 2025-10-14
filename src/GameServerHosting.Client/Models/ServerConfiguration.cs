using System.ComponentModel.DataAnnotations;

namespace GameServerHosting.Client.Models;

/// <summary>
/// Server configuration details for checkout
/// </summary>
public class ServerConfiguration
{
    public Guid CartItemId { get; set; }

    [Required(ErrorMessage = "Server name is required")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Server name must be between 3 and 50 characters")]
    public string ServerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Server region is required")]
    public string Region { get; set; } = string.Empty;

    public string? Description { get; set; }

    // Server settings
    public int MaxPlayers { get; set; } = 20;

    public string? Password { get; set; }

    public bool IsPublic { get; set; } = true;

    public bool AutoStart { get; set; } = true;

    public bool AutoUpdate { get; set; } = true;

    public bool BackupEnabled { get; set; } = true;

    public int BackupFrequencyHours { get; set; } = 24;

    // Advanced settings
    public Dictionary<string, string> CustomSettings { get; set; } = new();
}
