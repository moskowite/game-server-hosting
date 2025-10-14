using System.ComponentModel.DataAnnotations;

namespace GameServerHosting.Api.Models;

/// <summary>
/// Registration request model
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// Email address
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Username
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Password
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Password confirmation
    /// </summary>
    [Required]
    [Compare("Password", ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;

    /// <summary>
    /// First name
    /// </summary>
    [Required]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Last name
    /// </summary>
    [Required]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;
}
