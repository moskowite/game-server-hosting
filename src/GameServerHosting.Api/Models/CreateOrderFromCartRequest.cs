using GameServerHosting.Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace GameServerHosting.Api.Models;

/// <summary>
/// Request model for creating an order from cart items
/// </summary>
public class CreateOrderFromCartRequest
{
    /// <summary>
    /// Payment method to use
    /// </summary>
    [Required(ErrorMessage = "Payment method is required")]
    public PaymentMethod PaymentMethod { get; set; }

    /// <summary>
    /// Billing information as JSON string
    /// </summary>
    [Required(ErrorMessage = "Billing information is required")]
    public string BillingDetails { get; set; } = string.Empty;
}
