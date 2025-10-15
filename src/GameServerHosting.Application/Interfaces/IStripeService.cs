using Stripe;
using Stripe.Checkout;

namespace GameServerHosting.Application.Interfaces;

/// <summary>
/// Service interface for Stripe payment processing
/// </summary>
public interface IStripeService
{
    /// <summary>
    /// Creates a Stripe checkout session for purchasing a server plan
    /// </summary>
    /// <param name="serverPlanId">ID of the server plan to purchase</param>
    /// <param name="userId">ID of the user making the purchase</param>
    /// <param name="quantity">Number of servers to purchase (default: 1)</param>
    /// <returns>The created checkout session</returns>
    Task<Session> CreateCheckoutSessionAsync(Guid serverPlanId, Guid userId, int quantity = 1);

    /// <summary>
    /// Retrieves a Stripe checkout session by ID
    /// </summary>
    /// <param name="sessionId">The checkout session ID</param>
    /// <returns>The checkout session</returns>
    Task<Session> GetCheckoutSessionAsync(string sessionId);

    /// <summary>
    /// Creates a Stripe customer for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="email">The user's email address</param>
    /// <param name="name">The user's name</param>
    /// <returns>The created Stripe customer</returns>
    Task<Customer> CreateCustomerAsync(Guid userId, string email, string name);

    /// <summary>
    /// Retrieves a Stripe customer by ID
    /// </summary>
    /// <param name="customerId">The Stripe customer ID</param>
    /// <returns>The customer</returns>
    Task<Customer> GetCustomerAsync(string customerId);

    /// <summary>
    /// Constructs a webhook event from the request
    /// </summary>
    /// <param name="json">The webhook JSON payload</param>
    /// <param name="stripeSignature">The Stripe signature header</param>
    /// <returns>The constructed webhook event</returns>
    Event ConstructWebhookEvent(string json, string stripeSignature);

    /// <summary>
    /// Retrieves a payment intent by ID
    /// </summary>
    /// <param name="paymentIntentId">The payment intent ID</param>
    /// <returns>The payment intent</returns>
    Task<PaymentIntent> GetPaymentIntentAsync(string paymentIntentId);
}
