using GameServerHosting.Shared.Enums;

namespace GameServerHosting.Shared.Entities;

/// <summary>
/// Represents a purchase order and payment transaction
/// </summary>
public class Order
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Customer user ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Associated server ID (null for failed orders)
    /// </summary>
    public Guid? ServerId { get; set; }

    /// <summary>
    /// Server plan ID (billing plan selected)
    /// </summary>
    public Guid? ServerPlanId { get; set; }

    /// <summary>
    /// Human-readable order number (e.g., ORD-20251013-00001)
    /// </summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// Type of order
    /// </summary>
    public OrderType OrderType { get; set; }

    /// <summary>
    /// Current order status
    /// </summary>
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    /// <summary>
    /// Total order amount
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Currency code (ISO 4217)
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Payment method used
    /// </summary>
    public PaymentMethod PaymentMethod { get; set; }

    /// <summary>
    /// External payment transaction ID
    /// </summary>
    public string? PaymentTransactionId { get; set; }

    /// <summary>
    /// Payment completion timestamp
    /// </summary>
    public DateTime? PaymentDate { get; set; }

    /// <summary>
    /// Billing address and information (JSON)
    /// </summary>
    public string BillingDetails { get; set; } = "{}";

    /// <summary>
    /// Order creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // Navigation properties

    /// <summary>
    /// Customer who placed the order
    /// </summary>
    public ApplicationUser User { get; set; } = null!;

    /// <summary>
    /// Associated game server (if applicable)
    /// </summary>
    public GameServer? GameServer { get; set; }

    /// <summary>
    /// Server plan selected for this order
    /// </summary>
    public ServerPlan? ServerPlan { get; set; }

    /// <summary>
    /// Payment transactions for this order
    /// </summary>
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
