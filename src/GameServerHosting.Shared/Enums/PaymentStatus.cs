namespace GameServerHosting.Shared.Enums;

/// <summary>
/// Status of a payment transaction
/// </summary>
public enum PaymentStatus
{
    /// <summary>
    /// Payment initiated, awaiting processing
    /// </summary>
    Pending,

    /// <summary>
    /// Payment is being processed by payment gateway
    /// </summary>
    Processing,

    /// <summary>
    /// Payment successfully completed
    /// </summary>
    Completed,

    /// <summary>
    /// Payment failed due to insufficient funds or declined
    /// </summary>
    Failed,

    /// <summary>
    /// Payment cancelled by user or system
    /// </summary>
    Cancelled,

    /// <summary>
    /// Payment refunded to customer
    /// </summary>
    Refunded,

    /// <summary>
    /// Payment is under review for fraud or compliance
    /// </summary>
    UnderReview,

    /// <summary>
    /// Payment expired without completion
    /// </summary>
    Expired
}
