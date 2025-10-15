namespace GameServerHosting.Client.Models.Auth;

/// <summary>
/// Event arguments for authentication state changes
/// </summary>
public class AuthStateChangedEventArgs : EventArgs
{
    public AuthState AuthState { get; set; }

    public AuthStateChangedEventArgs(AuthState authState)
    {
        AuthState = authState;
    }
}
