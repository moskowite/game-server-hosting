using GameServerHosting.Infrastructure.Data;
using GameServerHosting.Application.DTOs;
using GameServerHosting.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace GameServerHosting.Infrastructure.Services;

/// <summary>
/// Service for handling Stripe payment processing
/// </summary>
public class StripeService : IStripeService
{
    private readonly StripeSettings _stripeSettings;
    private readonly GameServerDbContext _context;
    private readonly ILogger<StripeService> _logger;

    public StripeService(
        IOptions<StripeSettings> stripeSettings,
        GameServerDbContext context,
        ILogger<StripeService> logger)
    {
        _stripeSettings = stripeSettings.Value;
        _context = context;
        _logger = logger;

        // Set the Stripe API key
        StripeConfiguration.ApiKey = _stripeSettings.SecretKey;
    }

    /// <inheritdoc/>
    public async Task<Session> CreateCheckoutSessionAsync(Guid serverPlanId, Guid userId, int quantity = 1)
    {
        try
        {
            // Get the server plan from the database
            var serverPlan = await _context.ServerPlans
                .Include(sp => sp.ServerType)
                .FirstOrDefaultAsync(sp => sp.Id == serverPlanId);

            if (serverPlan == null)
            {
                throw new ArgumentException($"Server plan with ID {serverPlanId} not found");
            }

            // Create checkout session options
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = _stripeSettings.Currency,
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"{serverPlan.ServerType?.Name} - {serverPlan.Name}",
                                Description = serverPlan.Description,
                            },
                            UnitAmount = (long)(serverPlan.Price * 100), // Convert to cents
                        },
                        Quantity = quantity,
                    },
                },
                Mode = "payment",
                SuccessUrl = _stripeSettings.SuccessUrl + "?session_id={CHECKOUT_SESSION_ID}",
                CancelUrl = _stripeSettings.CancelUrl,
                ClientReferenceId = userId.ToString(),
                Metadata = new Dictionary<string, string>
                {
                    { "server_plan_id", serverPlanId.ToString() },
                    { "user_id", userId.ToString() },
                    { "quantity", quantity.ToString() }
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            _logger.LogInformation(
                "Created Stripe checkout session {SessionId} for user {UserId} and server plan {ServerPlanId}",
                session.Id, userId, serverPlanId);

            return session;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating checkout session for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Session> GetCheckoutSessionAsync(string sessionId)
    {
        try
        {
            var service = new SessionService();
            var session = await service.GetAsync(sessionId);

            _logger.LogDebug("Retrieved Stripe checkout session {SessionId}", sessionId);

            return session;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error retrieving checkout session {SessionId}", sessionId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Customer> CreateCustomerAsync(Guid userId, string email, string name)
    {
        try
        {
            var options = new CustomerCreateOptions
            {
                Email = email,
                Name = name,
                Metadata = new Dictionary<string, string>
                {
                    { "user_id", userId.ToString() }
                }
            };

            var service = new CustomerService();
            var customer = await service.CreateAsync(options);

            _logger.LogInformation(
                "Created Stripe customer {CustomerId} for user {UserId}",
                customer.Id, userId);

            return customer;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating customer for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Customer> GetCustomerAsync(string customerId)
    {
        try
        {
            var service = new CustomerService();
            var customer = await service.GetAsync(customerId);

            _logger.LogDebug("Retrieved Stripe customer {CustomerId}", customerId);

            return customer;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error retrieving customer {CustomerId}", customerId);
            throw;
        }
    }

    /// <inheritdoc/>
    public Event ConstructWebhookEvent(string json, string stripeSignature)
    {
        try
        {
            var webhookEvent = EventUtility.ConstructEvent(
                json,
                stripeSignature,
                _stripeSettings.WebhookSecret
            );

            _logger.LogInformation("Constructed webhook event of type {EventType}", webhookEvent.Type);

            return webhookEvent;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error constructing webhook event");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<PaymentIntent> GetPaymentIntentAsync(string paymentIntentId)
    {
        try
        {
            var service = new PaymentIntentService();
            var paymentIntent = await service.GetAsync(paymentIntentId);

            _logger.LogDebug("Retrieved payment intent {PaymentIntentId}", paymentIntentId);

            return paymentIntent;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error retrieving payment intent {PaymentIntentId}", paymentIntentId);
            throw;
        }
    }
}
