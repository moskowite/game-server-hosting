namespace GameServerHosting.Domain.Enums;

/// <summary>
/// Type of order transaction
/// </summary>
public enum OrderType
{
    /// <summary>
    /// New server purchase
    /// </summary>
    NewServer,

    /// <summary>
    /// Server subscription renewal
    /// </summary>
    Renewal,

    /// <summary>
    /// Server plan upgrade
    /// </summary>
    Upgrade
}
