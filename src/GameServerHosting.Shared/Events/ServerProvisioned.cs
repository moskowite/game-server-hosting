namespace GameServerHosting.Shared.Events;

/// <summary>
/// Event published when a game server has been successfully provisioned
/// </summary>
public class ServerProvisioned : BaseEvent
{
    /// <summary>
    /// The ID of the game server that was provisioned
    /// </summary>
    public Guid GameServerId { get; set; }

    /// <summary>
    /// The ID of the order associated with this provisioning
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// The IP address of the provisioned server
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// The port number the server is listening on
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// The cloud provider's unique identifier for this server
    /// </summary>
    public string? ProviderServerId { get; set; }

    /// <summary>
    /// The region where the server was provisioned
    /// </summary>
    public string Region { get; set; } = string.Empty;

    /// <summary>
    /// When the server was successfully deployed
    /// </summary>
    public DateTime DeployedAt { get; set; }
}
