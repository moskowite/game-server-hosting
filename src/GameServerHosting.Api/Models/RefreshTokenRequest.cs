using System.ComponentModel.DataAnnotations;

namespace GameServerHosting.Api.Models;

/// <summary>
/// Refresh token request model
/// </summary>
public class RefreshTokenRequest
{
    /// <summary>
    /// The refresh token
    /// </summary>
    [Required]
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Optional access token for validation
    /// </summary>
    public string? AccessToken { get; set; }
}
