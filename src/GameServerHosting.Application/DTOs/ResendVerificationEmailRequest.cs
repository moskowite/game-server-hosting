using System.ComponentModel.DataAnnotations;

namespace GameServerHosting.Application.DTOs;

/// <summary>
/// Request model for resending verification email
/// </summary>
public class ResendVerificationEmailRequest
{
    /// <summary>
    /// User email address
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
