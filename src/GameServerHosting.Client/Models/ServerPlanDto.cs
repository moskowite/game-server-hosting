using GameServerHosting.Domain.Enums;

namespace GameServerHosting.Client.Models;

/// <summary>
/// Data transfer object for server plan information
/// </summary>
public class ServerPlanDto
{
    public Guid Id { get; set; }
    public Guid ServerTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public BillingCycle BillingCycle { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public int DurationHours { get; set; }
    public decimal DiscountPercentage { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
