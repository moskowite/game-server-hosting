namespace GameServerHosting.Client.Models.Auth;

/// <summary>
/// Email verification request
/// </summary>
public class VerifyEmailRequest
{
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}
