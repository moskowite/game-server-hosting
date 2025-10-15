using GameServerHosting.Application.Interfaces;
using GameServerHosting.Application.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using System.Text.Json;

namespace GameServerHosting.Infrastructure.Providers.Payment;

/// <summary>
/// Stripe implementation of IPaymentGateway
/// </summary>
public class StripePaymentGateway : IPaymentGateway
{
    private readonly StripeSettings _settings;
    private readonly ILogger<StripePaymentGateway> _logger;

    public string ProviderName => "Stripe";

    public StripePaymentGateway(
        IOptions<StripeSettings> settings,
        ILogger<StripePaymentGateway> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        // Set the Stripe API key
        StripeConfiguration.ApiKey = _settings.SecretKey;
    }

    /// <inheritdoc/>
    public async Task<CheckoutSessionResult> CreateCheckoutSessionAsync(CheckoutSessionRequest request)
    {
        try
        {
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = request.LineItems.Select(item => new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = request.Currency,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Name,
                            Description = item.Description,
                        },
                        UnitAmount = item.UnitAmount,
                    },
                    Quantity = item.Quantity,
                }).ToList(),
                Mode = "payment",
                SuccessUrl = request.SuccessUrl,
                CancelUrl = request.CancelUrl,
                ClientReferenceId = request.UserId.ToString(),
                Metadata = request.Metadata
            };

            if (!string.IsNullOrEmpty(request.CustomerId))
            {
                options.Customer = request.CustomerId;
            }

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            _logger.LogInformation(
                "Created Stripe checkout session {SessionId} for user {UserId}",
                session.Id, request.UserId);

            return new CheckoutSessionResult
            {
                SessionId = session.Id,
                CheckoutUrl = session.Url,
                PaymentStatus = session.PaymentStatus,
                PaymentIntentId = session.PaymentIntentId,
                CustomerId = session.CustomerId,
                Amount = session.AmountTotal,
                Currency = session.Currency,
                Metadata = session.Metadata?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new()
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating checkout session");
            throw new InvalidOperationException($"Payment provider error: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<CheckoutSessionResult> GetCheckoutSessionAsync(string sessionId)
    {
        try
        {
            var service = new SessionService();
            var session = await service.GetAsync(sessionId);

            return new CheckoutSessionResult
            {
                SessionId = session.Id,
                CheckoutUrl = session.Url,
                PaymentStatus = session.PaymentStatus,
                PaymentIntentId = session.PaymentIntentId,
                CustomerId = session.CustomerId,
                Amount = session.AmountTotal,
                Currency = session.Currency,
                Metadata = session.Metadata?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new()
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error retrieving checkout session {SessionId}", sessionId);
            throw new InvalidOperationException($"Payment provider error: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<PaymentIntentResult> CreatePaymentIntentAsync(PaymentIntentRequest request)
    {
        try
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = request.Amount,
                Currency = request.Currency.ToLower(),
                Description = request.Description,
                Metadata = request.Metadata,
                CaptureMethod = request.AutoCapture ? "automatic" : "manual"
            };

            if (!string.IsNullOrEmpty(request.CustomerId))
            {
                options.Customer = request.CustomerId;
            }

            if (request.AutoCapture)
            {
                options.AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true,
                };
            }

            var service = new PaymentIntentService();
            var paymentIntent = await service.CreateAsync(options);

            _logger.LogInformation(
                "Created Stripe payment intent {PaymentIntentId}, amount: {Amount} {Currency}",
                paymentIntent.Id, request.Amount, request.Currency);

            return MapToPaymentIntentResult(paymentIntent);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating payment intent");
            throw new InvalidOperationException($"Payment provider error: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<PaymentIntentResult> ConfirmPaymentAsync(string paymentIntentId, string? paymentMethodId = null)
    {
        try
        {
            var service = new PaymentIntentService();
            
            // Get the payment intent first
            var paymentIntent = await service.GetAsync(paymentIntentId);

            // If already succeeded, return it
            if (paymentIntent.Status == "succeeded")
            {
                _logger.LogInformation(
                    "Payment intent {PaymentIntentId} already succeeded",
                    paymentIntentId);
                return MapToPaymentIntentResult(paymentIntent);
            }

            // Confirm the payment intent
            var options = new PaymentIntentConfirmOptions();
            
            if (!string.IsNullOrEmpty(paymentMethodId))
            {
                options.PaymentMethod = paymentMethodId;
            }

            paymentIntent = await service.ConfirmAsync(paymentIntentId, options);

            _logger.LogInformation(
                "Confirmed payment intent {PaymentIntentId}, status: {Status}",
                paymentIntentId, paymentIntent.Status);

            return MapToPaymentIntentResult(paymentIntent);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error confirming payment intent {PaymentIntentId}", paymentIntentId);
            throw new InvalidOperationException($"Payment provider error: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<PaymentIntentResult> CapturePaymentAsync(string paymentIntentId)
    {
        try
        {
            var service = new PaymentIntentService();
            var paymentIntent = await service.CaptureAsync(paymentIntentId);

            _logger.LogInformation(
                "Captured payment intent {PaymentIntentId}, amount: {Amount}",
                paymentIntentId, paymentIntent.Amount);

            return MapToPaymentIntentResult(paymentIntent);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error capturing payment intent {PaymentIntentId}", paymentIntentId);
            throw new InvalidOperationException($"Payment provider error: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<RefundResult> RefundPaymentAsync(RefundRequest request)
    {
        try
        {
            var options = new RefundCreateOptions
            {
                PaymentIntent = request.PaymentIntentId,
                Metadata = request.Metadata
            };

            if (request.Amount.HasValue)
            {
                options.Amount = request.Amount.Value;
            }

            if (!string.IsNullOrEmpty(request.Reason))
            {
                options.Reason = request.Reason.ToLower() switch
                {
                    "duplicate" => "duplicate",
                    "fraudulent" => "fraudulent",
                    _ => "requested_by_customer"
                };
            }

            var service = new RefundService();
            var refund = await service.CreateAsync(options);

            _logger.LogInformation(
                "Created refund {RefundId} for payment intent {PaymentIntentId}, amount: {Amount}",
                refund.Id, request.PaymentIntentId, refund.Amount);

            return new RefundResult
            {
                RefundId = refund.Id,
                PaymentIntentId = refund.PaymentIntentId,
                Amount = refund.Amount,
                Currency = refund.Currency,
                Status = refund.Status,
                Reason = refund.Reason,
                CreatedAt = refund.Created
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating refund for {PaymentIntentId}", request.PaymentIntentId);
            throw new InvalidOperationException($"Payment provider error: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public WebhookEvent VerifyWebhook(string payload, string signature, string secret)
    {
        try
        {
            var stripeEvent = EventUtility.ConstructEvent(payload, signature, secret);

            _logger.LogInformation("Verified Stripe webhook event {EventId} of type {EventType}", 
                stripeEvent.Id, stripeEvent.Type);

            return new WebhookEvent
            {
                EventId = stripeEvent.Id,
                EventType = stripeEvent.Type,
                DataJson = JsonSerializer.Serialize(stripeEvent.Data.Object),
                CreatedAt = stripeEvent.Created
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error verifying webhook signature");
            throw new InvalidOperationException($"Invalid webhook signature: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<CustomerResult> CreateCustomerAsync(CustomerRequest request)
    {
        try
        {
            var options = new CustomerCreateOptions
            {
                Email = request.Email,
                Name = request.Name,
                Metadata = request.Metadata
            };

            var service = new CustomerService();
            var customer = await service.CreateAsync(options);

            _logger.LogInformation("Created Stripe customer {CustomerId}", customer.Id);

            return new CustomerResult
            {
                CustomerId = customer.Id,
                Email = customer.Email,
                Name = customer.Name,
                Metadata = customer.Metadata?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new(),
                CreatedAt = customer.Created
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating customer");
            throw new InvalidOperationException($"Payment provider error: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<CustomerResult> GetCustomerAsync(string customerId)
    {
        try
        {
            var service = new CustomerService();
            var customer = await service.GetAsync(customerId);

            return new CustomerResult
            {
                CustomerId = customer.Id,
                Email = customer.Email,
                Name = customer.Name,
                Metadata = customer.Metadata?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new(),
                CreatedAt = customer.Created
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error retrieving customer {CustomerId}", customerId);
            throw new InvalidOperationException($"Payment provider error: {ex.Message}", ex);
        }
    }

    #region Private Helper Methods

    private PaymentIntentResult MapToPaymentIntentResult(PaymentIntent paymentIntent)
    {
        return new PaymentIntentResult
        {
            PaymentIntentId = paymentIntent.Id,
            ClientSecret = paymentIntent.ClientSecret,
            Status = paymentIntent.Status,
            Amount = paymentIntent.Amount,
            Currency = paymentIntent.Currency,
            CustomerId = paymentIntent.CustomerId,
            ErrorMessage = paymentIntent.LastPaymentError?.Message,
            Metadata = paymentIntent.Metadata?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new(),
            CreatedAt = paymentIntent.Created
        };
    }

    #endregion
}
