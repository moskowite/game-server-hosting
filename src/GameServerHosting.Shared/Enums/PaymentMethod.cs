namespace GameServerHosting.Shared.Enums;

/// <summary>
/// Payment method used for transaction
/// </summary>
public enum PaymentMethod
{
    /// <summary>
    /// Credit or debit card
    /// </summary>
    CreditCard,
    
    /// <summary>
    /// PayPal payment
    /// </summary>
    PayPal,
    
    /// <summary>
    /// Stripe payment processor
    /// </summary>
    Stripe,
    
    /// <summary>
    /// Cryptocurrency payment
    /// </summary>
    Crypto
}
