namespace GameServerHosting.Application.DTOs;

/// <summary>
/// JWT configuration settings
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// Secret key for signing tokens
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Token issuer
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Token audience
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Token expiration time in minutes
    /// </summary>
    public int ExpirationInMinutes { get; set; } = 60;

    /// <summary>
    /// Refresh token expiration in days
    /// </summary>
    public int RefreshTokenExpirationInDays { get; set; } = 7;
}
