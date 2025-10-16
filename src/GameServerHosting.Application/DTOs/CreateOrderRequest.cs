using System.ComponentModel.DataAnnotations;
using GameServerHosting.Domain.Enums;

namespace GameServerHosting.Application.DTOs;

/// <summary>
/// Request model for creating a new order
/// </summary>
public class CreateOrderRequest
{
    /// <summary>
    /// Server plan ID for the order
    /// </summary>
    [Required(ErrorMessage = "Server plan ID is required")]
    public Guid ServerPlanId { get; set; }

    /// <summary>
    /// Type of order (NewServer, Renewal, Upgrade)
    /// </summary>
    [Required(ErrorMessage = "Order type is required")]
    public OrderType OrderType { get; set; }

    /// <summary>
    /// Payment method to use
    /// </summary>
    [Required(ErrorMessage = "Payment method is required")]
    public PaymentMethod PaymentMethod { get; set; }

    /// <summary>
    /// Billing details (JSON format)
    /// </summary>
    public string? BillingDetails { get; set; }

    /// <summary>
    /// Associated server ID (for renewals and upgrades)
    /// </summary>
    public Guid? ServerId { get; set; }
}
