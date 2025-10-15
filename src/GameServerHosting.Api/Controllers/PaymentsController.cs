using GameServerHosting.Application.Interfaces;
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
    [HttpPost("webhook")]
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
    [HttpPost("create-intent")]
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
    [HttpPost("confirm")]
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
    /// Gets a payment by ID
    /// </summary>
    /// <param name="id">The payment ID</param>
    /// <returns>The payment details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(PaymentDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPayment(Guid id)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning("Invalid or missing user ID in token");
                return Unauthorized(new { error = "Invalid user authentication" });
            }

            var payment = await _paymentService.GetPaymentByIdAsync(id);

            if (payment == null)
            {
                _logger.LogWarning("Payment {PaymentId} not found", id);
                return NotFound(new { error = "Payment not found" });
            }

            // Ensure user can only access their own payments
            if (payment.UserId != userId)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to access payment {PaymentId} belonging to user {PaymentUserId}",
                    userId, id, payment.UserId);
                return NotFound(new { error = "Payment not found" });
            }

            return Ok(new PaymentDetailsResponse
            {
                Id = payment.Id,
                OrderId = payment.OrderId,
                UserId = payment.UserId,
                Amount = payment.Amount,
                Currency = payment.Currency,
                PaymentMethod = payment.PaymentMethod.ToString(),
                Status = payment.Status.ToString(),
                TransactionId = payment.TransactionId,
                PaymentGateway = payment.PaymentGateway,
                PaymentIdentifier = payment.PaymentIdentifier,
                ErrorMessage = payment.ErrorMessage,
                ErrorCode = payment.ErrorCode,
                CreatedAt = payment.CreatedAt,
                UpdatedAt = payment.UpdatedAt,
                CompletedAt = payment.CompletedAt,
                RefundedAt = payment.RefundedAt,
                RefundAmount = payment.RefundAmount,
                RefundReason = payment.RefundReason
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving payment {PaymentId}", id);
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

/// <summary>
/// Response model for payment details
/// </summary>
public class PaymentDetailsResponse
{
    /// <summary>
    /// Payment ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Associated order ID
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// User ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Payment amount
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Currency code
    /// </summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Payment method
    /// </summary>
    public string PaymentMethod { get; set; } = string.Empty;

    /// <summary>
    /// Payment status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Transaction ID from payment gateway
    /// </summary>
    public string? TransactionId { get; set; }

    /// <summary>
    /// Payment gateway name
    /// </summary>
    public string? PaymentGateway { get; set; }

    /// <summary>
    /// Payment identifier (last 4 digits of card, etc.)
    /// </summary>
    public string? PaymentIdentifier { get; set; }

    /// <summary>
    /// Error message if payment failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Error code from payment gateway
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Payment creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Payment completion timestamp
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Refund timestamp
    /// </summary>
    public DateTime? RefundedAt { get; set; }

    /// <summary>
    /// Refund amount
    /// </summary>
    public decimal? RefundAmount { get; set; }

    /// <summary>
    /// Refund reason
    /// </summary>
    public string? RefundReason { get; set; }
}

#endregion
