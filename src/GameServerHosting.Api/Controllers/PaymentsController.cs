using GameServerHosting.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using System.Security.Claims;

namespace GameServerHosting.Api.Controllers;

/// <summary>
/// Controller for handling payment operations with Stripe
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IStripeService _stripeService;
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IStripeService stripeService,
        IPaymentService paymentService,
        ILogger<PaymentsController> logger)
    {
        _stripeService = stripeService;
        _paymentService = paymentService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a Stripe checkout session for purchasing a server plan
    /// </summary>
    /// <param name="request">The checkout session request</param>
    /// <returns>The checkout session with URL to redirect the user</returns>
    [HttpPost("create-checkout-session")]
    [ProducesResponseType(typeof(CreateCheckoutSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateCheckoutSession([FromBody] CreateCheckoutSessionRequest request)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning("Invalid or missing user ID in token");
                return Unauthorized(new { error = "Invalid user authentication" });
            }

            if (request.Quantity < 1)
            {
                return BadRequest(new { error = "Quantity must be at least 1" });
            }

            var session = await _stripeService.CreateCheckoutSessionAsync(
                request.ServerPlanId,
                userId,
                request.Quantity);

            return Ok(new CreateCheckoutSessionResponse
            {
                SessionId = session.Id,
                CheckoutUrl = session.Url
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid server plan ID: {ServerPlanId}", request.ServerPlanId);
            return BadRequest(new { error = ex.Message });
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating checkout session");
            return StatusCode(500, new { error = "Payment processing error. Please try again." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating checkout session");
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Retrieves a checkout session by ID
    /// </summary>
    /// <param name="sessionId">The Stripe checkout session ID</param>
    /// <returns>The checkout session details</returns>
    [HttpGet("checkout-session/{sessionId}")]
    [ProducesResponseType(typeof(CheckoutSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCheckoutSession(string sessionId)
    {
        try
        {
            var session = await _stripeService.GetCheckoutSessionAsync(sessionId);

            return Ok(new CheckoutSessionResponse
            {
                SessionId = session.Id,
                Status = session.Status,
                PaymentStatus = session.PaymentStatus,
                AmountTotal = session.AmountTotal,
                Currency = session.Currency,
                CustomerEmail = session.CustomerDetails?.Email
            });
        }
        catch (StripeException ex) when (ex.StripeError.Code == "resource_missing")
        {
            _logger.LogWarning("Checkout session not found: {SessionId}", sessionId);
            return NotFound(new { error = "Checkout session not found" });
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error retrieving checkout session");
            return StatusCode(500, new { error = "Payment processing error. Please try again." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving checkout session");
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Webhook endpoint for Stripe events
    /// </summary>
    /// <returns>Success status</returns>
    [HttpPost("/api/webhooks/stripe")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StripeWebhook()
    {
        try
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var stripeSignature = Request.Headers["Stripe-Signature"].ToString();

            if (string.IsNullOrEmpty(stripeSignature))
            {
                _logger.LogWarning("Missing Stripe signature header");
                return BadRequest(new { error = "Missing signature" });
            }

            var stripeEvent = _stripeService.ConstructWebhookEvent(json, stripeSignature);

            _logger.LogInformation("Processing Stripe webhook event: {EventType}", stripeEvent.Type);

            // Handle the event based on type
            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                    var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
                    if (session != null)
                    {
                        await _paymentService.HandleCheckoutSessionCompletedAsync(session);
                    }
                    else
                    {
                        _logger.LogWarning("Checkout session data is null for event {EventId}", stripeEvent.Id);
                    }
                    break;

                case "payment_intent.succeeded":
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    if (paymentIntent != null)
                    {
                        await _paymentService.HandlePaymentIntentSucceededAsync(paymentIntent);
                    }
                    else
                    {
                        _logger.LogWarning("Payment intent data is null for event {EventId}", stripeEvent.Id);
                    }
                    break;

                case "payment_intent.payment_failed":
                    var failedPaymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    if (failedPaymentIntent != null)
                    {
                        await _paymentService.HandlePaymentIntentFailedAsync(failedPaymentIntent);
                    }
                    else
                    {
                        _logger.LogWarning("Failed payment intent data is null for event {EventId}", stripeEvent.Id);
                    }
                    break;

                case "charge.refunded":
                    var charge = stripeEvent.Data.Object as Stripe.Charge;
                    if (charge != null)
                    {
                        await _paymentService.HandleChargeRefundedAsync(charge);
                    }
                    else
                    {
                        _logger.LogWarning("Charge data is null for event {EventId}", stripeEvent.Id);
                    }
                    break;

                default:
                    _logger.LogInformation("Unhandled event type: {EventType}", stripeEvent.Type);
                    break;
            }

            return Ok();
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe webhook signature verification failed");
            return BadRequest(new { error = "Invalid signature" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe webhook");
            return StatusCode(500, new { error = "Webhook processing error" });
        }
    }

    /// <summary>
    /// Creates a payment intent for direct payment processing
    /// </summary>
    /// <param name="request">The payment intent request</param>
    /// <returns>The created payment intent with client secret</returns>
    [HttpPost("create-payment-intent")]
    [ProducesResponseType(typeof(CreatePaymentIntentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreatePaymentIntent([FromBody] CreatePaymentIntentRequest request)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning("Invalid or missing user ID in token");
                return Unauthorized(new { error = "Invalid user authentication" });
            }

            if (request.Amount <= 0)
            {
                return BadRequest(new { error = "Amount must be greater than 0" });
            }

            var paymentIntent = await _paymentService.CreatePaymentIntentAsync(
                userId,
                request.Amount,
                request.Currency ?? "usd",
                request.OrderId,
                request.Metadata);

            return Ok(new CreatePaymentIntentResponse
            {
                PaymentIntentId = paymentIntent.Id,
                ClientSecret = paymentIntent.ClientSecret,
                Amount = paymentIntent.Amount,
                Currency = paymentIntent.Currency,
                Status = paymentIntent.Status
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request for creating payment intent");
            return BadRequest(new { error = ex.Message });
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating payment intent");
            return StatusCode(500, new { error = "Payment processing error. Please try again." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating payment intent");
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Confirms a payment intent
    /// </summary>
    /// <param name="request">The payment intent confirmation request</param>
    /// <returns>The confirmed payment intent</returns>
    [HttpPost("confirm-payment")]
    [ProducesResponseType(typeof(ConfirmPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ConfirmPayment([FromBody] ConfirmPaymentRequest request)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning("Invalid or missing user ID in token");
                return Unauthorized(new { error = "Invalid user authentication" });
            }

            if (string.IsNullOrEmpty(request.PaymentIntentId))
            {
                return BadRequest(new { error = "Payment intent ID is required" });
            }

            var paymentIntent = await _paymentService.ConfirmPaymentAsync(
                request.PaymentIntentId,
                request.PaymentMethodId);

            return Ok(new ConfirmPaymentResponse
            {
                PaymentIntentId = paymentIntent.Id,
                Status = paymentIntent.Status,
                Amount = paymentIntent.Amount,
                Currency = paymentIntent.Currency
            });
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error confirming payment");
            return StatusCode(500, new { error = "Payment processing error. Please try again." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error confirming payment");
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Refunds a payment
    /// </summary>
    /// <param name="request">The refund request</param>
    /// <returns>The refund details</returns>
    [HttpPost("refund")]
    [ProducesResponseType(typeof(RefundPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RefundPayment([FromBody] RefundPaymentRequest request)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning("Invalid or missing user ID in token");
                return Unauthorized(new { error = "Invalid user authentication" });
            }

            if (string.IsNullOrEmpty(request.PaymentIntentId))
            {
                return BadRequest(new { error = "Payment intent ID is required" });
            }

            var refund = await _paymentService.RefundPaymentAsync(
                request.PaymentIntentId,
                request.Amount,
                request.Reason);

            return Ok(new RefundPaymentResponse
            {
                RefundId = refund.Id,
                PaymentIntentId = refund.PaymentIntentId,
                Amount = refund.Amount,
                Currency = refund.Currency,
                Status = refund.Status,
                Reason = refund.Reason
            });
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error processing refund");
            return StatusCode(500, new { error = "Refund processing error. Please try again." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing refund");
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }
}

#region Request/Response Models

/// <summary>
/// Request model for creating a checkout session
/// </summary>
public class CreateCheckoutSessionRequest
{
    /// <summary>
    /// ID of the server plan to purchase
    /// </summary>
    public Guid ServerPlanId { get; set; }

    /// <summary>
    /// Quantity of servers to purchase
    /// </summary>
    public int Quantity { get; set; } = 1;
}

/// <summary>
/// Response model for create checkout session
/// </summary>
public class CreateCheckoutSessionResponse
{
    /// <summary>
    /// The Stripe checkout session ID
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// The URL to redirect the user to for checkout
    /// </summary>
    public string CheckoutUrl { get; set; } = string.Empty;
}

/// <summary>
/// Response model for checkout session retrieval
/// </summary>
public class CheckoutSessionResponse
{
    /// <summary>
    /// The Stripe checkout session ID
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// The session status
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// The payment status
    /// </summary>
    public string? PaymentStatus { get; set; }

    /// <summary>
    /// Total amount in cents
    /// </summary>
    public long? AmountTotal { get; set; }

    /// <summary>
    /// Currency code
    /// </summary>
    public string? Currency { get; set; }

    /// <summary>
    /// Customer email address
    /// </summary>
    public string? CustomerEmail { get; set; }
}

/// <summary>
/// Request model for creating a payment intent
/// </summary>
public class CreatePaymentIntentRequest
{
    /// <summary>
    /// Payment amount in smallest currency unit (e.g., cents)
    /// </summary>
    public long Amount { get; set; }

    /// <summary>
    /// Currency code (e.g., "usd", "eur")
    /// </summary>
    public string? Currency { get; set; }

    /// <summary>
    /// Associated order ID (optional)
    /// </summary>
    public Guid? OrderId { get; set; }

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Response model for creating a payment intent
/// </summary>
public class CreatePaymentIntentResponse
{
    /// <summary>
    /// The payment intent ID
    /// </summary>
    public string PaymentIntentId { get; set; } = string.Empty;

    /// <summary>
    /// Client secret for confirming the payment
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Payment amount in smallest currency unit
    /// </summary>
    public long Amount { get; set; }

    /// <summary>
    /// Currency code
    /// </summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Payment intent status
    /// </summary>
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Request model for confirming a payment
/// </summary>
public class ConfirmPaymentRequest
{
    /// <summary>
    /// The payment intent ID to confirm
    /// </summary>
    public string PaymentIntentId { get; set; } = string.Empty;

    /// <summary>
    /// The payment method ID (optional if already attached)
    /// </summary>
    public string? PaymentMethodId { get; set; }
}

/// <summary>
/// Response model for confirming a payment
/// </summary>
public class ConfirmPaymentResponse
{
    /// <summary>
    /// The payment intent ID
    /// </summary>
    public string PaymentIntentId { get; set; } = string.Empty;

    /// <summary>
    /// Payment status after confirmation
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Payment amount
    /// </summary>
    public long Amount { get; set; }

    /// <summary>
    /// Currency code
    /// </summary>
    public string Currency { get; set; } = string.Empty;
}

/// <summary>
/// Request model for refunding a payment
/// </summary>
public class RefundPaymentRequest
{
    /// <summary>
    /// The payment intent ID to refund
    /// </summary>
    public string PaymentIntentId { get; set; } = string.Empty;

    /// <summary>
    /// Amount to refund in smallest currency unit (null for full refund)
    /// </summary>
    public long? Amount { get; set; }

    /// <summary>
    /// Reason for the refund
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Response model for refunding a payment
/// </summary>
public class RefundPaymentResponse
{
    /// <summary>
    /// The refund ID
    /// </summary>
    public string RefundId { get; set; } = string.Empty;

    /// <summary>
    /// The payment intent ID that was refunded
    /// </summary>
    public string? PaymentIntentId { get; set; }

    /// <summary>
    /// Refund amount
    /// </summary>
    public long Amount { get; set; }

    /// <summary>
    /// Currency code
    /// </summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Refund status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Reason for the refund
    /// </summary>
    public string? Reason { get; set; }
}

#endregion
