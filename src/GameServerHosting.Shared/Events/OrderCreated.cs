namespace GameServerHosting.Shared.Events;

/// <summary>
/// Event published when an order is created
/// </summary>
public class OrderCreated : BaseEvent
{
    /// <summary>
    /// The ID of the created order
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// The order number
    /// </summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the server plan being ordered
    /// </summary>
    public Guid ServerPlanId { get; set; }

    /// <summary>
    /// Order type (e.g., NewServer, Renewal, Upgrade)
    /// </summary>
    public string OrderType { get; set; } = string.Empty;

    /// <summary>
    /// Total amount of the order
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Currency code
    /// </summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Payment method
    /// </summary>
    public string PaymentMethod { get; set; } = string.Empty;

    /// <summary>
    /// Server ID (if upgrading/renewing existing server)
    /// </summary>
    public Guid? ServerId { get; set; }
}
