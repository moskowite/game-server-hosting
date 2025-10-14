using System.ComponentModel.DataAnnotations;

namespace GameServerHosting.Api.Models;

/// <summary>
/// Request model for email verification
/// </summary>
public class VerifyEmailRequest
{
    /// <summary>
    /// User email address
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Email verification token
    /// </summary>
    [Required]
    public string Token { get; set; } = string.Empty;
}
