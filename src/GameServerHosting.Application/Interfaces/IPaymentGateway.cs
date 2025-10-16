namespace GameServerHosting.Application.Interfaces;

/// <summary>
/// Provider-agnostic payment gateway interface
/// Abstracts payment processing from specific implementations (Stripe, PayPal, etc.)
/// </summary>
public interface IPaymentGateway
{
    /// <summary>
    /// Gets the name of the payment provider (e.g., "Stripe", "PayPal")
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Creates a checkout session for customer payment
    /// </summary>
    /// <param name="request">Checkout session request details</param>
    /// <returns>Checkout session response with session ID and redirect URL</returns>
    Task<CheckoutSessionResult> CreateCheckoutSessionAsync(CheckoutSessionRequest request);

    /// <summary>
    /// Retrieves a checkout session by ID
    /// </summary>
    /// <param name="sessionId">The checkout session ID</param>
    /// <returns>Checkout session details</returns>
    Task<CheckoutSessionResult> GetCheckoutSessionAsync(string sessionId);

    /// <summary>
    /// Creates a payment intent for direct payment processing
    /// </summary>
    /// <param name="request">Payment intent request details</param>
    /// <returns>Payment intent result with client secret</returns>
    Task<PaymentIntentResult> CreatePaymentIntentAsync(PaymentIntentRequest request);

    /// <summary>
    /// Confirms a payment intent
    /// </summary>
    /// <param name="paymentIntentId">The payment intent ID</param>
    /// <param name="paymentMethodId">Optional payment method ID</param>
    /// <returns>Updated payment intent result</returns>
    Task<PaymentIntentResult> ConfirmPaymentAsync(string paymentIntentId, string? paymentMethodId = null);

    /// <summary>
    /// Captures a previously authorized payment
    /// </summary>
    /// <param name="paymentIntentId">The payment intent ID</param>
    /// <returns>Captured payment intent result</returns>
    Task<PaymentIntentResult> CapturePaymentAsync(string paymentIntentId);

    /// <summary>
    /// Refunds a payment
    /// </summary>
    /// <param name="request">Refund request details</param>
    /// <returns>Refund result</returns>
    Task<RefundResult> RefundPaymentAsync(RefundRequest request);

    /// <summary>
    /// Verifies a webhook signature and constructs the event
    /// </summary>
    /// <param name="payload">The webhook JSON payload</param>
    /// <param name="signature">The webhook signature header</param>
    /// <param name="secret">The webhook secret</param>
    /// <returns>Webhook event details</returns>
    WebhookEvent VerifyWebhook(string payload, string signature, string secret);

    /// <summary>
    /// Creates a customer in the payment provider
    /// </summary>
    /// <param name="request">Customer creation request</param>
    /// <returns>Customer result with provider customer ID</returns>
    Task<CustomerResult> CreateCustomerAsync(CustomerRequest request);

    /// <summary>
    /// Retrieves a customer by ID
    /// </summary>
    /// <param name="customerId">The provider's customer ID</param>
    /// <returns>Customer details</returns>
    Task<CustomerResult> GetCustomerAsync(string customerId);
}

/// <summary>
/// Request for creating a checkout session
/// </summary>
public class CheckoutSessionRequest
{
    /// <summary>
    /// User ID making the purchase
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Line items to purchase
    /// </summary>
    public List<CheckoutLineItem> LineItems { get; set; } = new();

    /// <summary>
    /// Currency code (e.g., "usd", "eur")
    /// </summary>
    public string Currency { get; set; } = "usd";

    /// <summary>
    /// Success redirect URL
    /// </summary>
    public string SuccessUrl { get; set; } = string.Empty;

    /// <summary>
    /// Cancel redirect URL
    /// </summary>
    public string CancelUrl { get; set; } = string.Empty;

    /// <summary>
    /// Optional customer ID if customer exists
    /// </summary>
    public string? CustomerId { get; set; }

    /// <summary>
    /// Custom metadata for the session
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Line item for checkout
/// </summary>
public class CheckoutLineItem
{
    /// <summary>
    /// Product/service name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Product/service description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Quantity to purchase
    /// </summary>
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Unit price in smallest currency unit (e.g., cents)
    /// </summary>
    public long UnitAmount { get; set; }
}

/// <summary>
/// Request for creating a payment intent
/// </summary>
public class PaymentIntentRequest
{
    /// <summary>
    /// Payment amount in smallest currency unit (e.g., cents)
    /// </summary>
    public long Amount { get; set; }

    /// <summary>
    /// Currency code (e.g., "usd", "eur")
    /// </summary>
    public string Currency { get; set; } = "usd";

    /// <summary>
    /// Customer ID if available
    /// </summary>
    public string? CustomerId { get; set; }

    /// <summary>
    /// Description of the payment
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Custom metadata
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Whether to capture payment automatically (true) or require manual capture (false)
    /// </summary>
    public bool AutoCapture { get; set; } = true;
}

/// <summary>
/// Request for refunding a payment
/// </summary>
public class RefundRequest
{
    /// <summary>
    /// Payment intent/transaction ID to refund
    /// </summary>
    public string PaymentIntentId { get; set; } = string.Empty;

    /// <summary>
    /// Amount to refund (null for full refund)
    /// </summary>
    public long? Amount { get; set; }

    /// <summary>
    /// Reason for the refund
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Custom metadata
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Request for creating a customer
/// </summary>
public class CustomerRequest
{
    /// <summary>
    /// Customer email
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Customer name
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Custom metadata
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Result of creating/retrieving a checkout session
/// </summary>
public class CheckoutSessionResult
{
    /// <summary>
    /// Provider-specific session ID
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// URL to redirect customer for payment
    /// </summary>
    public string? CheckoutUrl { get; set; }

    /// <summary>
    /// Payment status (unpaid, paid, etc.)
    /// </summary>
    public string PaymentStatus { get; set; } = string.Empty;

    /// <summary>
    /// Associated payment intent ID
    /// </summary>
    public string? PaymentIntentId { get; set; }

    /// <summary>
    /// Customer ID
    /// </summary>
    public string? CustomerId { get; set; }

    /// <summary>
    /// Total amount
    /// </summary>
    public long? Amount { get; set; }

    /// <summary>
    /// Currency
    /// </summary>
    public string? Currency { get; set; }

    /// <summary>
    /// Custom metadata
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Result of payment intent operations
/// </summary>
public class PaymentIntentResult
{
    /// <summary>
    /// Payment intent ID
    /// </summary>
    public string PaymentIntentId { get; set; } = string.Empty;

    /// <summary>
    /// Client secret for confirming payment on client side
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Payment status (requires_payment_method, requires_confirmation, succeeded, etc.)
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Payment amount in smallest currency unit
    /// </summary>
    public long Amount { get; set; }

    /// <summary>
    /// Currency code
    /// </summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Customer ID
    /// </summary>
    public string? CustomerId { get; set; }

    /// <summary>
    /// Error message if failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Custom metadata
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Result of refund operation
/// </summary>
public class RefundResult
{
    /// <summary>
    /// Refund ID
    /// </summary>
    public string RefundId { get; set; } = string.Empty;

    /// <summary>
    /// Payment intent ID that was refunded
    /// </summary>
    public string PaymentIntentId { get; set; } = string.Empty;

    /// <summary>
    /// Refunded amount
    /// </summary>
    public long Amount { get; set; }

    /// <summary>
    /// Currency
    /// </summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Refund status (pending, succeeded, failed, cancelled)
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Reason for refund
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Result of customer operations
/// </summary>
public class CustomerResult
{
    /// <summary>
    /// Provider's customer ID
    /// </summary>
    public string CustomerId { get; set; } = string.Empty;

    /// <summary>
    /// Customer email
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Customer name
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Custom metadata
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Webhook event data
/// </summary>
public class WebhookEvent
{
    /// <summary>
    /// Event type (e.g., "checkout.session.completed", "payment_intent.succeeded")
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Event ID
    /// </summary>
    public string EventId { get; set; } = string.Empty;

    /// <summary>
    /// Event data as JSON string
    /// </summary>
    public string DataJson { get; set; } = string.Empty;

    /// <summary>
    /// Event creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
