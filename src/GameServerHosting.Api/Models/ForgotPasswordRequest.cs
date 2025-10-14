using System.ComponentModel.DataAnnotations;

namespace GameServerHosting.Api.Models;

/// <summary>
/// Forgot password request model
/// </summary>
public class ForgotPasswordRequest
{
    /// <summary>
    /// Email address to send password reset link
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
