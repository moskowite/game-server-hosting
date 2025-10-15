using System.ComponentModel.DataAnnotations;

namespace GameServerHosting.Client.Models.Auth;

/// <summary>
/// Request to resend email verification
/// </summary>
public class ResendVerificationEmailRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string Email { get; set; } = string.Empty;
}
