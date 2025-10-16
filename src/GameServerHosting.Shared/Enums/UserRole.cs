namespace GameServerHosting.Shared.Enums;

/// <summary>
/// User role enumeration for authorization
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Regular user with standard permissions
    /// </summary>
    User,

    /// <summary>
    /// Administrator with full system access
    /// </summary>
    Admin,

    /// <summary>
    /// Moderator with elevated permissions
    /// </summary>
    Moderator
}
