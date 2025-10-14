using GameServerHosting.Shared.Enums;

namespace GameServerHosting.Shared.Entities;

/// <summary>
/// Represents an item in a user's shopping cart
/// </summary>
public class CartItem
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
    public int Quantity { get; set; } = 1;

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

    // Navigation properties

    /// <summary>
    /// User who owns this cart item
    /// </summary>
    public ApplicationUser User { get; set; } = null!;

    /// <summary>
    /// Server plan in the cart
    /// </summary>
    public ServerPlan ServerPlan { get; set; } = null!;
}
