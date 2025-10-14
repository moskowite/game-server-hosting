using System.ComponentModel.DataAnnotations;

namespace GameServerHosting.Api.Models;

/// <summary>
/// Request model for updating an existing server type
/// </summary>
public class UpdateServerTypeRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string GameName { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "CPU cores must be greater than 0")]
    public int CpuCores { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "RAM must be greater than 0")]
    public int RamMb { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Disk space must be greater than 0")]
    public int DiskGb { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Max players must be greater than 0")]
    public int MaxPlayers { get; set; }

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Hourly price must be 0 or greater")]
    public decimal HourlyPrice { get; set; }

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Monthly price must be 0 or greater")]
    public decimal MonthlyPrice { get; set; }

    [Required]
    [MaxLength(255)]
    public string DockerImage { get; set; } = string.Empty;

    public string DefaultConfig { get; set; } = "{}";

    public bool IsActive { get; set; } = true;
}
