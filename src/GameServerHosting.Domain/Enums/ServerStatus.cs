namespace GameServerHosting.Domain.Enums;

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
    /// Creating cloud infrastructure (droplet, VM, etc.)
    /// </summary>
    Provisioning,

    /// <summary>
    /// Installing software and configuring server
    /// </summary>
    Configuring,

    /// <summary>
    /// Launching game server application
    /// </summary>
    Starting,

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
    /// Provisioning or startup failed
    /// </summary>
    Failed,

    /// <summary>
    /// Server permanently deleted
    /// </summary>
    Terminated
}
