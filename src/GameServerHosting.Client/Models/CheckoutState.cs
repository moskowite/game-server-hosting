using GameServerHosting.Application.DTOs;

namespace GameServerHosting.Client.Models;

/// <summary>
/// Represents the current state of the checkout process
/// </summary>
public class CheckoutState
{
    public int CurrentStep { get; set; } = 1;
    
    public List<CartItemDto> CartItems { get; set; } = new();
    
    public Dictionary<Guid, ServerConfiguration> ServerConfigurations { get; set; } = new();
    
    public BillingDetails? BillingDetails { get; set; }
    
    public PaymentDetails? PaymentDetails { get; set; }
    
    public OrderDto? CompletedOrder { get; set; }
    
    public decimal Subtotal { get; set; }
    
    public decimal Tax { get; set; }
    
    public decimal Discount { get; set; }
    
    public decimal Total { get; set; }
    
    public string? PromoCode { get; set; }

    public DateTime? StartedAt { get; set; }
    
    public DateTime? CompletedAt { get; set; }

    public bool IsComplete => CurrentStep > 5;

    public bool CanProceedToNextStep()
    {
        return CurrentStep switch
        {
            1 => CartItems.Any(),
            2 => ServerConfigurations.Count == CartItems.Count && ServerConfigurations.All(sc => !string.IsNullOrWhiteSpace(sc.Value.ServerName)),
            3 => BillingDetails != null && !string.IsNullOrWhiteSpace(BillingDetails.Email),
            4 => PaymentDetails != null && PaymentDetails.AgreeToTerms,
            5 => CompletedOrder != null,
            _ => false
        };
    }
}
