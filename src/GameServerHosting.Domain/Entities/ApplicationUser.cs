using GameServerHosting.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace GameServerHosting.Domain.Entities;

/// <summary>
/// Custom application user that extends IdentityUser with additional properties
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>
    /// User's first name
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// User's last name
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// User's role in the system
    /// </summary>
    public UserRole Role { get; set; } = UserRole.User;

    /// <summary>
    /// Whether the account is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Account creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Last successful login timestamp
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    // Navigation properties

    /// <summary>
    /// Game servers owned by this user
    /// </summary>
    public ICollection<GameServer> GameServers { get; set; } = new List<GameServer>();

    /// <summary>
    /// Orders placed by this user
    /// </summary>
    public ICollection<Order> Orders { get; set; } = new List<Order>();

    /// <summary>
    /// Payment transactions made by this user
    /// </summary>
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
