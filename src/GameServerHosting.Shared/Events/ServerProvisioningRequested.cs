namespace GameServerHosting.Shared.Events;

/// <summary>
/// Event published when a user requests a new game server to be provisioned
/// </summary>
public class ServerProvisioningRequested : BaseEvent
{
    /// <summary>
    /// The ID of the game server entity that will be provisioned
    /// </summary>
    public Guid GameServerId { get; set; }

    /// <summary>
    /// The ID of the server plan being provisioned
    /// </summary>
    public Guid ServerPlanId { get; set; }

    /// <summary>
    /// The ID of the order associated with this provisioning request
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// The region where the server should be provisioned
    /// </summary>
    public string Region { get; set; } = string.Empty;

    /// <summary>
    /// The cloud provider to use for provisioning
    /// </summary>
    public string CloudProvider { get; set; } = "DigitalOcean";

    /// <summary>
    /// Server name/hostname
    /// </summary>
    public string ServerName { get; set; } = string.Empty;

    /// <summary>
    /// Game type/server type ID
    /// </summary>
    public Guid ServerTypeId { get; set; }
}
