namespace GameServerHosting.Client.Models.Auth;

/// <summary>
/// Request to refresh an access token
/// </summary>
public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
