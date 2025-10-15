using GameServerHosting.Application.DTOs;
using GameServerHosting.Domain.Entities;

namespace GameServerHosting.Application.Interfaces;

/// <summary>
/// Service interface for JWT token generation and validation
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates a JWT access token for the specified user
    /// </summary>
    /// <param name="user">The user to generate a token for</param>
    /// <param name="rememberMe">Whether to extend token lifetime</param>
    /// <returns>The generated JWT token</returns>
    string GenerateAccessToken(ApplicationUser user, bool rememberMe = false);

    /// <summary>
    /// Generates a refresh token
    /// </summary>
    /// <returns>A unique refresh token string</returns>
    string GenerateRefreshToken();

    /// <summary>
    /// Validates a JWT token and returns the user ID
    /// </summary>
    /// <param name="token">The token to validate</param>
    /// <returns>User ID if valid, null otherwise</returns>
    Guid? ValidateToken(string token);
}
