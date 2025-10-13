using GameServerHosting.Shared.Enums;

namespace GameServerHosting.Shared.Entities;

/// <summary>
/// Represents a payment transaction for an order
/// </summary>
public class Payment
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Associated order ID
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// User who made the payment
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Payment amount
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Currency code (ISO 4217)
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Payment method used
    /// </summary>
    public PaymentMethod PaymentMethod { get; set; }

    /// <summary>
    /// Current payment status
    /// </summary>
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    /// <summary>
    /// External payment gateway transaction ID
    /// </summary>
    public string? TransactionId { get; set; }

    /// <summary>
    /// Payment gateway used (e.g., "Stripe", "PayPal")
    /// </summary>
    public string? PaymentGateway { get; set; }

    /// <summary>
    /// Last 4 digits of card or payment identifier
    /// </summary>
    public string? PaymentIdentifier { get; set; }

    /// <summary>
    /// Payment gateway response data (JSON)
    /// </summary>
    public string GatewayResponse { get; set; } = "{}";

    /// <summary>
    /// Error message if payment failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Error code from payment gateway
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Payment initiation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last status update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Payment completion timestamp
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Refund timestamp (if applicable)
    /// </summary>
    public DateTime? RefundedAt { get; set; }

    /// <summary>
    /// Refund amount (if partial refund)
    /// </summary>
    public decimal? RefundAmount { get; set; }

    /// <summary>
    /// Refund reason
    /// </summary>
    public string? RefundReason { get; set; }

    /// <summary>
    /// IP address from which payment was initiated
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent of the browser/client
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Additional metadata (JSON)
    /// </summary>
    public string Metadata { get; set; } = "{}";

    // Navigation properties

    /// <summary>
    /// Order this payment is for
    /// </summary>
    public Order Order { get; set; } = null!;

    /// <summary>
    /// User who made the payment
    /// </summary>
    public User User { get; set; } = null!;
}
