using GameServerHosting.Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace GameServerHosting.Api.Models;

/// <summary>
/// Request model for updating order status
/// </summary>
public class UpdateOrderStatusRequest
{
    /// <summary>
    /// New status for the order
    /// </summary>
    [Required(ErrorMessage = "Status is required")]
    public OrderStatus Status { get; set; }

    /// <summary>
    /// Optional payment transaction ID
    /// </summary>
    public string? PaymentTransactionId { get; set; }
}
