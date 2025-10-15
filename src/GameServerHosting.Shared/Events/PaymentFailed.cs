namespace GameServerHosting.Shared.Events;

/// <summary>
/// Event published when a payment attempt fails
/// </summary>
public class PaymentFailed : BaseEvent
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
    /// Amount that failed to process
    /// </summary>
    public long Amount { get; set; }

    /// <summary>
    /// Currency code
    /// </summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Error message from payment processor
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Error code from payment processor
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Whether this failure is retryable
    /// </summary>
    public bool IsRetryable { get; set; }
}
