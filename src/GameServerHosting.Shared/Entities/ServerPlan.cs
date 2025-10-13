using GameServerHosting.Shared.Enums;

namespace GameServerHosting.Shared.Entities;

/// <summary>
/// Represents a billing plan for a server type (e.g., hourly, monthly)
/// </summary>
public class ServerPlan
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Associated server type ID
    /// </summary>
    public Guid ServerTypeId { get; set; }

    /// <summary>
    /// Plan name (e.g., "Hourly", "Monthly", "Quarterly")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Billing cycle for this plan
    /// </summary>
    public BillingCycle BillingCycle { get; set; }

    /// <summary>
    /// Price for this billing cycle
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Currency code (ISO 4217)
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Duration in hours for this plan
    /// </summary>
    public int DurationHours { get; set; }

    /// <summary>
    /// Discount percentage compared to hourly rate (0-100)
    /// </summary>
    public decimal DiscountPercentage { get; set; }

    /// <summary>
    /// Whether this plan is available for purchase
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Display order for sorting plans
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Plan description and benefits
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // Navigation properties

    /// <summary>
    /// Server type this plan belongs to
    /// </summary>
    public ServerType ServerType { get; set; } = null!;

    /// <summary>
    /// Orders using this plan
    /// </summary>
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
