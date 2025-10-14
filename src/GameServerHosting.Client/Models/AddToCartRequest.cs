using System.ComponentModel.DataAnnotations;

namespace GameServerHosting.Client.Models;

/// <summary>
/// Request model for adding item to cart
/// </summary>
public class AddToCartRequest
{
    /// <summary>
    /// Server plan ID to add to cart
    /// </summary>
    [Required]
    public Guid ServerPlanId { get; set; }

    /// <summary>
    /// Quantity to add
    /// </summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; } = 1;
}
