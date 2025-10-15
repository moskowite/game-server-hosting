using System.ComponentModel.DataAnnotations;

namespace GameServerHosting.Application.DTOs;

/// <summary>
/// Request model for updating cart item quantity
/// </summary>
public class UpdateCartItemRequest
{
    /// <summary>
    /// New quantity for the cart item
    /// </summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; }
}
