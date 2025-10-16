using System.ComponentModel.DataAnnotations;
using GameServerHosting.Domain.Enums;

namespace GameServerHosting.Client.Models;

/// <summary>
/// Payment information for checkout
/// </summary>
public class PaymentDetails
{
    [Required(ErrorMessage = "Payment method is required")]
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CreditCard;

    // Credit Card fields
    [Required(ErrorMessage = "Card number is required")]
    public string CardNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Cardholder name is required")]
    public string CardholderName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Expiration month is required")]
    [Range(1, 12, ErrorMessage = "Month must be between 1 and 12")]
    public int ExpirationMonth { get; set; }

    [Required(ErrorMessage = "Expiration year is required")]
    public int ExpirationYear { get; set; }

    [Required(ErrorMessage = "CVV is required")]
    [StringLength(4, MinimumLength = 3, ErrorMessage = "CVV must be 3 or 4 digits")]
    public string CVV { get; set; } = string.Empty;

    // PayPal fields
    public string? PayPalEmail { get; set; }

    // Bank Transfer fields
    public string? BankName { get; set; }
    public string? AccountNumber { get; set; }
    public string? RoutingNumber { get; set; }

    // Cryptocurrency fields
    public string? CryptoWalletAddress { get; set; }
    public string? CryptoType { get; set; }

    // Common fields
    public bool SavePaymentMethod { get; set; } = false;
    public bool AgreeToTerms { get; set; } = false;
}
