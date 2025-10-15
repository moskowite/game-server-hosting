namespace GameServerHosting.Shared.Events;

/// <summary>
/// Event published when a payment has been successfully captured
/// </summary>
public class PaymentCaptured : BaseEvent
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
    /// Amount captured in smallest currency unit (e.g., cents)
    /// </summary>
    public long Amount { get; set; }

    /// <summary>
    /// Currency code (e.g., "usd")
    /// </summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Transaction ID from the payment gateway
    /// </summary>
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>
    /// When the payment was captured
    /// </summary>
    public DateTime CapturedAt { get; set; }
}
