using GameServerHosting.Application.Interfaces;
using GameServerHosting.Domain.Entities;
using GameServerHosting.Domain.Enums;
using GameServerHosting.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GameServerHosting.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for User entity operations
/// </summary>
public class UserRepository : Repository<ApplicationUser>, IUserRepository
{
    public UserRepository(GameServerDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ApplicationUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(u => u.UserName == username, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ApplicationUser>> GetActiveUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(u => u.IsActive)
            .OrderBy(u => u.UserName)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ApplicationUser>> GetUsersByRoleAsync(UserRole role, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(u => u.Role == role)
            .OrderBy(u => u.UserName)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ApplicationUser?> GetWithPreferencesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(u => u.UserPreferences)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ApplicationUser?> GetWithServersAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(u => u.GameServers)
                .ThenInclude(gs => gs.ServerType)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
    }
}
