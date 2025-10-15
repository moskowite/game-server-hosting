namespace GameServerHosting.Client.Models.Auth;

/// <summary>
/// Current authentication state
/// </summary>
public class AuthState
{
    public bool IsAuthenticated { get; set; }
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? TokenExpiresAt { get; set; }
}
