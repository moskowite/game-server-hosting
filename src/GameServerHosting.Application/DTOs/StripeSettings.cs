namespace GameServerHosting.Application.DTOs;

/// <summary>
/// Stripe payment service configuration settings
/// </summary>
public class StripeSettings
{
    /// <summary>
    /// Stripe secret key (sk_test_... for test mode, sk_live_... for production)
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Stripe publishable key (pk_test_... for test mode, pk_live_... for production)
    /// </summary>
    public string PublishableKey { get; set; } = string.Empty;

    /// <summary>
    /// Stripe webhook secret for validating webhook events
    /// </summary>
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>
    /// Default currency for payments (e.g., "usd", "eur", "gbp")
    /// </summary>
    public string Currency { get; set; } = "usd";

    /// <summary>
    /// URL to redirect to after successful payment
    /// </summary>
    public string SuccessUrl { get; set; } = string.Empty;

    /// <summary>
    /// URL to redirect to after cancelled payment
    /// </summary>
    public string CancelUrl { get; set; } = string.Empty;
}
