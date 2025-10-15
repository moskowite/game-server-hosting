namespace GameServerHosting.Shared.Events;

/// <summary>
/// Event published when a payment has been authorized (but not yet captured)
/// </summary>
public class PaymentAuthorized : BaseEvent
{
    /// <summary>
    /// The ID of the payment entity
    /// </summary>
    public Guid PaymentId { get; set; }

    /// <summary>
    /// The ID of the order associated with this payment
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// Payment intent ID from Stripe
    /// </summary>
    public string PaymentIntentId { get; set; } = string.Empty;

    /// <summary>
    /// Amount authorized in smallest currency unit (e.g., cents)
    /// </summary>
    public long Amount { get; set; }

    /// <summary>
    /// Currency code (e.g., "usd")
    /// </summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Payment method used
    /// </summary>
    public string PaymentMethod { get; set; } = string.Empty;
}
