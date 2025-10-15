using System.ComponentModel.DataAnnotations;

namespace GameServerHosting.Client.Models.Auth;

/// <summary>
/// Forgot password request
/// </summary>
public class ForgotPasswordRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string Email { get; set; } = string.Empty;
}
