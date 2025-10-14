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
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IStripeService stripeService,
        ILogger<PaymentsController> logger)
    {
        _stripeService = stripeService;
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
                    await HandleCheckoutSessionCompleted(session);
                    break;

                case "payment_intent.succeeded":
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    await HandlePaymentIntentSucceeded(paymentIntent);
                    break;

                case "payment_intent.payment_failed":
                    var failedPaymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    await HandlePaymentIntentFailed(failedPaymentIntent);
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

    private async Task HandleCheckoutSessionCompleted(Stripe.Checkout.Session? session)
    {
        if (session == null)
        {
            _logger.LogWarning("Checkout session is null");
            return;
        }

        _logger.LogInformation(
            "Checkout session completed: {SessionId}, PaymentStatus: {PaymentStatus}",
            session.Id,
            session.PaymentStatus);

        // TODO: Implement order fulfillment logic
        // 1. Create an order record in the database
        // 2. Provision the game server
        // 3. Send confirmation email to customer
        // 4. Update user's active subscriptions

        await Task.CompletedTask;
    }

    private async Task HandlePaymentIntentSucceeded(PaymentIntent? paymentIntent)
    {
        if (paymentIntent == null)
        {
            _logger.LogWarning("Payment intent is null");
            return;
        }

        _logger.LogInformation(
            "Payment intent succeeded: {PaymentIntentId}, Amount: {Amount}",
            paymentIntent.Id,
            paymentIntent.Amount);

        // TODO: Implement additional payment success logic if needed

        await Task.CompletedTask;
    }

    private async Task HandlePaymentIntentFailed(PaymentIntent? paymentIntent)
    {
        if (paymentIntent == null)
        {
            _logger.LogWarning("Payment intent is null");
            return;
        }

        _logger.LogWarning(
            "Payment intent failed: {PaymentIntentId}, LastPaymentError: {Error}",
            paymentIntent.Id,
            paymentIntent.LastPaymentError?.Message);

        // TODO: Implement payment failure logic
        // 1. Notify the customer
        // 2. Update order status
        // 3. Log for manual review if needed

        await Task.CompletedTask;
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

#endregion
