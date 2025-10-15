using GameServerHosting.Domain.Entities;
using Stripe;

namespace GameServerHosting.Application.Interfaces;

/// <summary>
/// Service interface for payment processing operations
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Creates a payment intent for direct payment processing
    /// </summary>
    /// <param name="userId">ID of the user making the payment</param>
    /// <param name="amount">Payment amount in the smallest currency unit (e.g., cents)</param>
    /// <param name="currency">Currency code (e.g., "usd", "eur")</param>
    /// <param name="orderId">Associated order ID</param>
    /// <param name="metadata">Additional metadata for the payment</param>
    /// <returns>The created payment intent</returns>
    Task<PaymentIntent> CreatePaymentIntentAsync(
        Guid userId, 
        long amount, 
        string currency,
        Guid? orderId = null,
        Dictionary<string, string>? metadata = null);

    /// <summary>
    /// Confirms a payment intent and processes the payment
    /// </summary>
    /// <param name="paymentIntentId">The payment intent ID to confirm</param>
    /// <param name="paymentMethodId">The payment method ID to use</param>
    /// <returns>The confirmed payment intent</returns>
    Task<PaymentIntent> ConfirmPaymentAsync(string paymentIntentId, string? paymentMethodId = null);

    /// <summary>
    /// Refunds a payment
    /// </summary>
    /// <param name="paymentIntentId">The payment intent ID to refund</param>
    /// <param name="amount">Amount to refund (null for full refund)</param>
    /// <param name="reason">Reason for the refund</param>
    /// <returns>The created refund</returns>
    Task<Refund> RefundPaymentAsync(string paymentIntentId, long? amount = null, string? reason = null);

    /// <summary>
    /// Handles webhook event for checkout session completed
    /// </summary>
    /// <param name="session">The checkout session that was completed</param>
    Task HandleCheckoutSessionCompletedAsync(Stripe.Checkout.Session session);

    /// <summary>
    /// Handles webhook event for payment intent succeeded
    /// </summary>
    /// <param name="paymentIntent">The payment intent that succeeded</param>
    Task HandlePaymentIntentSucceededAsync(PaymentIntent paymentIntent);

    /// <summary>
    /// Handles webhook event for payment intent failed
    /// </summary>
    /// <param name="paymentIntent">The payment intent that failed</param>
    Task HandlePaymentIntentFailedAsync(PaymentIntent paymentIntent);

    /// <summary>
    /// Handles webhook event for charge refunded
    /// </summary>
    /// <param name="charge">The charge that was refunded</param>
    Task HandleChargeRefundedAsync(Charge charge);

    /// <summary>
    /// Creates or updates a payment record in the database
    /// </summary>
    /// <param name="paymentIntent">The Stripe payment intent</param>
    /// <param name="orderId">Associated order ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>The created or updated payment record</returns>
    Task<Payment> CreateOrUpdatePaymentRecordAsync(PaymentIntent paymentIntent, Guid? orderId, Guid userId);

    /// <summary>
    /// Gets a payment by transaction ID
    /// </summary>
    /// <param name="transactionId">The Stripe payment intent ID</param>
    /// <returns>The payment record or null if not found</returns>
    Task<Payment?> GetPaymentByTransactionIdAsync(string transactionId);

    /// <summary>
    /// Gets a payment by ID
    /// </summary>
    /// <param name="paymentId">The payment ID</param>
    /// <returns>The payment record or null if not found</returns>
    Task<Payment?> GetPaymentByIdAsync(Guid paymentId);
}
