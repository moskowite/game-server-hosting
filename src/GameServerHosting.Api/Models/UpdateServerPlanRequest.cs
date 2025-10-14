using System.ComponentModel.DataAnnotations;
using GameServerHosting.Shared.Enums;

namespace GameServerHosting.Api.Models;

/// <summary>
/// Request model for updating an existing server plan
/// </summary>
public class UpdateServerPlanRequest
{
    /// <summary>
    /// Plan name (e.g., "Hourly", "Monthly", "Quarterly")
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Billing cycle for this plan
    /// </summary>
    [Required]
    public BillingCycle BillingCycle { get; set; }

    /// <summary>
    /// Price for this billing cycle
    /// </summary>
    [Required]
    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    /// <summary>
    /// Currency code (ISO 4217)
    /// </summary>
    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Duration in hours for this plan
    /// </summary>
    [Required]
    [Range(1, int.MaxValue)]
    public int DurationHours { get; set; }

    /// <summary>
    /// Discount percentage compared to hourly rate (0-100)
    /// </summary>
    [Range(0, 100)]
    public decimal DiscountPercentage { get; set; }

    /// <summary>
    /// Whether this plan is available for purchase
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Display order for sorting plans
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Plan description and benefits
    /// </summary>
    [Required]
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;
}
