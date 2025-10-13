namespace GameServerHosting.Shared.Enums;

/// <summary>
/// Game server operational status
/// </summary>
public enum ServerStatus
{
    /// <summary>
    /// Server order placed, awaiting provisioning
    /// </summary>
    Pending,
    
    /// <summary>
    /// Server is active and accessible
    /// </summary>
    Running,
    
    /// <summary>
    /// Server temporarily stopped by user
    /// </summary>
    Stopped,
    
    /// <summary>
    /// Server suspended (payment issue or expiration)
    /// </summary>
    Suspended,
    
    /// <summary>
    /// Server permanently deleted
    /// </summary>
    Terminated
}
