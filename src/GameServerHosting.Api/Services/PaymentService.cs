using GameServerHosting.Api.Data;
using GameServerHosting.Api.Models;
using GameServerHosting.Shared.Entities;
using GameServerHosting.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;
using System.Text.Json;

namespace GameServerHosting.Api.Services;

/// <summary>
/// Service for handling payment processing operations
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly StripeSettings _stripeSettings;
    private readonly GameServerDbContext _context;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IOptions<StripeSettings> stripeSettings,
        GameServerDbContext context,
        ILogger<PaymentService> logger)
    {
        _stripeSettings = stripeSettings.Value;
        _context = context;
        _logger = logger;

        // Set the Stripe API key
        StripeConfiguration.ApiKey = _stripeSettings.SecretKey;
    }

    /// <inheritdoc/>
    public async Task<PaymentIntent> CreatePaymentIntentAsync(
        Guid userId,
        long amount,
        string currency,
        Guid? orderId = null,
        Dictionary<string, string>? metadata = null)
    {
        try
        {
            // Validate user exists
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new ArgumentException($"User with ID {userId} not found");
            }

            // Build metadata
            var paymentMetadata = new Dictionary<string, string>
            {
                { "user_id", userId.ToString() }
            };

            if (orderId.HasValue)
            {
                paymentMetadata["order_id"] = orderId.Value.ToString();
            }

            if (metadata != null)
            {
                foreach (var kvp in metadata)
                {
                    paymentMetadata[kvp.Key] = kvp.Value;
                }
            }

            // Create payment intent options
            var options = new PaymentIntentCreateOptions
            {
                Amount = amount,
                Currency = currency.ToLower(),
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true,
                },
                Metadata = paymentMetadata,
                Description = orderId.HasValue 
                    ? $"Payment for Order {orderId.Value}" 
                    : "Payment for game server hosting",
            };

            var service = new PaymentIntentService();
            var paymentIntent = await service.CreateAsync(options);

            _logger.LogInformation(
                "Created payment intent {PaymentIntentId} for user {UserId}, amount: {Amount} {Currency}",
                paymentIntent.Id, userId, amount, currency);

            // Create payment record in database
            if (orderId.HasValue)
            {
                await CreateOrUpdatePaymentRecordAsync(paymentIntent, orderId, userId);
            }

            return paymentIntent;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating payment intent for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<PaymentIntent> ConfirmPaymentAsync(string paymentIntentId, string? paymentMethodId = null)
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
                return paymentIntent;
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

            // Update payment record in database
            if (paymentIntent.Metadata.TryGetValue("user_id", out var userIdStr) 
                && Guid.TryParse(userIdStr, out var userId))
            {
                Guid? orderId = null;
                if (paymentIntent.Metadata.TryGetValue("order_id", out var orderIdStr) 
                    && Guid.TryParse(orderIdStr, out var parsedOrderId))
                {
                    orderId = parsedOrderId;
                }

                await CreateOrUpdatePaymentRecordAsync(paymentIntent, orderId, userId);
            }

            return paymentIntent;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error confirming payment intent {PaymentIntentId}", paymentIntentId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Refund> RefundPaymentAsync(string paymentIntentId, long? amount = null, string? reason = null)
    {
        try
        {
            var options = new RefundCreateOptions
            {
                PaymentIntent = paymentIntentId,
            };

            if (amount.HasValue)
            {
                options.Amount = amount.Value;
            }

            if (!string.IsNullOrEmpty(reason))
            {
                options.Reason = reason.ToLower() switch
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
                refund.Id, paymentIntentId, refund.Amount);

            // Update payment record in database
            var payment = await GetPaymentByTransactionIdAsync(paymentIntentId);
            if (payment != null)
            {
                payment.Status = PaymentStatus.Refunded;
                payment.RefundedAt = DateTime.UtcNow;
                payment.RefundAmount = amount.HasValue ? amount.Value / 100m : payment.Amount;
                payment.RefundReason = reason ?? "Customer request";
                payment.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Updated payment record {PaymentId} with refund information",
                    payment.Id);
            }

            return refund;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating refund for payment intent {PaymentIntentId}", paymentIntentId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task HandleCheckoutSessionCompletedAsync(Stripe.Checkout.Session session)
    {
        try
        {
            if (session == null)
            {
                _logger.LogWarning("Checkout session is null");
                return;
            }

            _logger.LogInformation(
                "Processing checkout session completed: {SessionId}, PaymentStatus: {PaymentStatus}",
                session.Id,
                session.PaymentStatus);

            // Extract metadata
            if (!session.Metadata.TryGetValue("user_id", out var userIdStr) ||
                !Guid.TryParse(userIdStr, out var userId))
            {
                _logger.LogWarning("Missing or invalid user_id in session metadata");
                return;
            }

            if (!session.Metadata.TryGetValue("server_plan_id", out var serverPlanIdStr) ||
                !Guid.TryParse(serverPlanIdStr, out var serverPlanId))
            {
                _logger.LogWarning("Missing or invalid server_plan_id in session metadata");
                return;
            }

            if (!session.Metadata.TryGetValue("quantity", out var quantityStr) ||
                !int.TryParse(quantityStr, out var quantity))
            {
                quantity = 1;
            }

            // Get the payment intent from the session
            string? paymentIntentId = session.PaymentIntentId;
            if (string.IsNullOrEmpty(paymentIntentId))
            {
                _logger.LogWarning("No payment intent ID in checkout session");
                return;
            }

            // Create order if it doesn't exist
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.PaymentTransactionId == session.Id);

            if (order == null)
            {
                var serverPlan = await _context.ServerPlans
                    .Include(sp => sp.ServerType)
                    .FirstOrDefaultAsync(sp => sp.Id == serverPlanId);

                if (serverPlan == null)
                {
                    _logger.LogError("Server plan {ServerPlanId} not found", serverPlanId);
                    return;
                }

                order = new Order
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ServerPlanId = serverPlanId,
                    OrderNumber = GenerateOrderNumber(),
                    OrderType = OrderType.NewServer,
                    Status = session.PaymentStatus == "paid" ? OrderStatus.Completed : OrderStatus.Pending,
                    TotalAmount = serverPlan.Price * quantity,
                    Currency = session.Currency?.ToUpper() ?? "USD",
                    PaymentMethod = Shared.Enums.PaymentMethod.Stripe,
                    PaymentTransactionId = session.Id,
                    PaymentDate = session.PaymentStatus == "paid" ? DateTime.UtcNow : null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Created order {OrderId} from checkout session {SessionId}",
                    order.Id, session.Id);
            }
            else if (session.PaymentStatus == "paid" && order.Status != OrderStatus.Completed)
            {
                order.Status = OrderStatus.Completed;
                order.PaymentDate = DateTime.UtcNow;
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Updated order {OrderId} to completed status",
                    order.Id);
            }

            // Get the payment intent and create/update payment record
            var paymentIntentService = new PaymentIntentService();
            var paymentIntent = await paymentIntentService.GetAsync(paymentIntentId);
            await CreateOrUpdatePaymentRecordAsync(paymentIntent, order.Id, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling checkout session completed event");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task HandlePaymentIntentSucceededAsync(PaymentIntent paymentIntent)
    {
        try
        {
            if (paymentIntent == null)
            {
                _logger.LogWarning("Payment intent is null");
                return;
            }

            _logger.LogInformation(
                "Processing payment intent succeeded: {PaymentIntentId}, Amount: {Amount}",
                paymentIntent.Id,
                paymentIntent.Amount);

            // Extract user ID and order ID from metadata
            if (!paymentIntent.Metadata.TryGetValue("user_id", out var userIdStr) ||
                !Guid.TryParse(userIdStr, out var userId))
            {
                _logger.LogWarning("Missing or invalid user_id in payment intent metadata");
                return;
            }

            Guid? orderId = null;
            if (paymentIntent.Metadata.TryGetValue("order_id", out var orderIdStr) &&
                Guid.TryParse(orderIdStr, out var parsedOrderId))
            {
                orderId = parsedOrderId;
            }

            // Create or update payment record
            await CreateOrUpdatePaymentRecordAsync(paymentIntent, orderId, userId);

            // Update order status if applicable
            if (orderId.HasValue)
            {
                var order = await _context.Orders.FindAsync(orderId.Value);
                if (order != null && order.Status != OrderStatus.Completed)
                {
                    order.Status = OrderStatus.Completed;
                    order.PaymentDate = DateTime.UtcNow;
                    order.PaymentTransactionId = paymentIntent.Id;
                    order.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation(
                        "Updated order {OrderId} to completed status from payment intent",
                        orderId.Value);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling payment intent succeeded event");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task HandlePaymentIntentFailedAsync(PaymentIntent paymentIntent)
    {
        try
        {
            if (paymentIntent == null)
            {
                _logger.LogWarning("Payment intent is null");
                return;
            }

            _logger.LogWarning(
                "Processing payment intent failed: {PaymentIntentId}, Error: {Error}",
                paymentIntent.Id,
                paymentIntent.LastPaymentError?.Message);

            // Extract user ID and order ID from metadata
            if (!paymentIntent.Metadata.TryGetValue("user_id", out var userIdStr) ||
                !Guid.TryParse(userIdStr, out var userId))
            {
                _logger.LogWarning("Missing or invalid user_id in payment intent metadata");
                return;
            }

            Guid? orderId = null;
            if (paymentIntent.Metadata.TryGetValue("order_id", out var orderIdStr) &&
                Guid.TryParse(orderIdStr, out var parsedOrderId))
            {
                orderId = parsedOrderId;
            }

            // Create or update payment record with failed status
            await CreateOrUpdatePaymentRecordAsync(paymentIntent, orderId, userId);

            // Update order status if applicable
            if (orderId.HasValue)
            {
                var order = await _context.Orders.FindAsync(orderId.Value);
                if (order != null && order.Status != OrderStatus.Failed)
                {
                    order.Status = OrderStatus.Failed;
                    order.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation(
                        "Updated order {OrderId} to failed status",
                        orderId.Value);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling payment intent failed event");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task HandleChargeRefundedAsync(Charge charge)
    {
        try
        {
            if (charge == null)
            {
                _logger.LogWarning("Charge is null");
                return;
            }

            _logger.LogInformation(
                "Processing charge refunded: {ChargeId}, Amount: {Amount}",
                charge.Id,
                charge.AmountRefunded);

            // Find payment by charge ID or payment intent ID
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => 
                    p.TransactionId == charge.PaymentIntentId || 
                    p.TransactionId == charge.Id);

            if (payment == null)
            {
                _logger.LogWarning(
                    "Payment not found for charge {ChargeId}",
                    charge.Id);
                return;
            }

            // Update payment record with refund information
            payment.Status = charge.Refunded ? PaymentStatus.Refunded : PaymentStatus.Completed;
            payment.RefundedAt = charge.Refunded ? DateTime.UtcNow : null;
            payment.RefundAmount = charge.AmountRefunded / 100m;
            payment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Updated payment {PaymentId} with refund information",
                payment.Id);

            // Update associated order if exists
            if (payment.OrderId != Guid.Empty)
            {
                var order = await _context.Orders.FindAsync(payment.OrderId);
                if (order != null && charge.Refunded)
                {
                    order.Status = OrderStatus.Refunded;
                    order.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation(
                        "Updated order {OrderId} to refunded status",
                        order.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling charge refunded event");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Payment> CreateOrUpdatePaymentRecordAsync(
        PaymentIntent paymentIntent,
        Guid? orderId,
        Guid userId)
    {
        try
        {
            // Check if payment record already exists
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.TransactionId == paymentIntent.Id);

            var isNew = payment == null;

            if (isNew)
            {
                payment = new Payment
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId ?? Guid.Empty,
                    UserId = userId,
                    TransactionId = paymentIntent.Id,
                    PaymentGateway = "Stripe",
                    CreatedAt = DateTime.UtcNow
                };
                _context.Payments.Add(payment);
            }

            // Update payment details
            payment.Amount = paymentIntent.Amount / 100m; // Convert from cents to dollars
            payment.Currency = (paymentIntent.Currency ?? "usd").ToUpper();
            payment.Status = MapPaymentIntentStatus(paymentIntent.Status ?? "unknown");
            payment.PaymentMethod = Shared.Enums.PaymentMethod.Stripe;
            payment.UpdatedAt = DateTime.UtcNow;

            // Update completion timestamp if succeeded
            if (paymentIntent.Status == "succeeded" && payment.CompletedAt == null)
            {
                payment.CompletedAt = DateTime.UtcNow;
            }

            // Store error information if failed
            if (paymentIntent.LastPaymentError != null)
            {
                payment.ErrorMessage = paymentIntent.LastPaymentError.Message;
                payment.ErrorCode = paymentIntent.LastPaymentError.Code;
            }

            // Store payment method details if available
            if (paymentIntent.PaymentMethod != null)
            {
                // PaymentIntent.PaymentMethod can be either a string ID or an expanded object
                string? paymentMethodId = paymentIntent.PaymentMethod is string pmId 
                    ? pmId 
                    : (paymentIntent.PaymentMethod as Stripe.PaymentMethod)?.Id;
                
                if (!string.IsNullOrEmpty(paymentMethodId))
                {
                    try
                    {
                        var paymentMethodService = new PaymentMethodService();
                        var paymentMethod = await paymentMethodService.GetAsync(paymentMethodId);
                        
                        if (paymentMethod?.Card != null)
                        {
                            payment.PaymentIdentifier = $"**** {paymentMethod.Card.Last4}";
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not retrieve payment method details");
                    }
                }
            }

            // Store metadata
            if (paymentIntent.Metadata != null && paymentIntent.Metadata.Count > 0)
            {
                payment.Metadata = JsonSerializer.Serialize(paymentIntent.Metadata);
            }

            // Store gateway response
            payment.GatewayResponse = JsonSerializer.Serialize(new
            {
                paymentIntent.Id,
                paymentIntent.Status,
                paymentIntent.Amount,
                paymentIntent.Currency,
                paymentIntent.Created,
                paymentIntent.ClientSecret
            });

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "{Action} payment record {PaymentId} for payment intent {PaymentIntentId}",
                isNew ? "Created" : "Updated",
                payment.Id,
                paymentIntent.Id);

            return payment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating/updating payment record for payment intent {PaymentIntentId}", paymentIntent.Id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Payment?> GetPaymentByTransactionIdAsync(string transactionId)
    {
        return await _context.Payments
            .Include(p => p.Order)
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.TransactionId == transactionId);
    }

    #region Private Helper Methods

    private static PaymentStatus MapPaymentIntentStatus(string stripeStatus)
    {
        return stripeStatus switch
        {
            "requires_payment_method" => PaymentStatus.Pending,
            "requires_confirmation" => PaymentStatus.Pending,
            "requires_action" => PaymentStatus.Pending,
            "processing" => PaymentStatus.Processing,
            "requires_capture" => PaymentStatus.Processing,
            "canceled" => PaymentStatus.Cancelled,
            "succeeded" => PaymentStatus.Completed,
            _ => PaymentStatus.Failed
        };
    }

    private static string GenerateOrderNumber()
    {
        var timestamp = DateTime.UtcNow;
        var random = new Random().Next(10000, 99999);
        return $"ORD-{timestamp:yyyyMMdd}-{random}";
    }

    #endregion
}
