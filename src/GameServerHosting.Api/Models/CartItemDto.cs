namespace GameServerHosting.Api.Models;

/// <summary>
/// Data transfer object for cart item information
/// </summary>
public class CartItemDto
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User ID who owns this cart item
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Server plan ID being added to cart
    /// </summary>
    public Guid ServerPlanId { get; set; }

    /// <summary>
    /// Quantity of this server plan
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Price at the time item was added to cart
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Currency code (ISO 4217)
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Server plan details
    /// </summary>
    public ServerPlanDto? ServerPlan { get; set; }
}
