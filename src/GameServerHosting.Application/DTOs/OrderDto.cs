using GameServerHosting.Domain.Enums;

namespace GameServerHosting.Application.DTOs;

/// <summary>
/// DTO for order information
/// </summary>
public class OrderDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public OrderType OrderType { get; set; }
    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public PaymentMethod PaymentMethod { get; set; }
    public DateTime? PaymentDate { get; set; }
    public DateTime CreatedAt { get; set; }

    // Server Information (if applicable)
    public string? ServerName { get; set; }
    public string? GameName { get; set; }

    // Plan Information (if applicable)
    public string? PlanName { get; set; }
}
