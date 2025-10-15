using GameServerHosting.Domain.Entities;
using GameServerHosting.Domain.Enums;

namespace GameServerHosting.Application.Interfaces;

/// <summary>
/// Repository interface for User entity operations
/// </summary>
public interface IUserRepository : IRepository<ApplicationUser>
{
    /// <summary>
    /// Gets a user by email
    /// </summary>
    Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by username
    /// </summary>
    Task<ApplicationUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active users
    /// </summary>
    Task<IEnumerable<ApplicationUser>> GetActiveUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets users by role
    /// </summary>
    Task<IEnumerable<ApplicationUser>> GetUsersByRoleAsync(UserRole role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user with their preferences
    /// </summary>
    Task<ApplicationUser?> GetWithPreferencesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user with their game servers
    /// </summary>
    Task<ApplicationUser?> GetWithServersAsync(Guid userId, CancellationToken cancellationToken = default);
}
