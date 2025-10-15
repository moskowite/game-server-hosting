namespace GameServerHosting.Client.Models.Auth;

/// <summary>
/// Result of an authentication operation
/// </summary>
public class AuthResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public List<string> Errors { get; set; } = new();
    public AuthResponse? Data { get; set; }

    public static AuthResult Successful(AuthResponse data, string? message = null)
    {
        return new AuthResult
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    public static AuthResult Failed(string message, List<string>? errors = null)
    {
        return new AuthResult
        {
            Success = false,
            Message = message,
            Errors = errors ?? new List<string>()
        };
    }
}
