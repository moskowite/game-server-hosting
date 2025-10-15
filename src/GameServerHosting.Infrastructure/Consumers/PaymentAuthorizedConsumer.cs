using GameServerHosting.Application.Interfaces;
using GameServerHosting.Domain.Enums;
using GameServerHosting.Shared.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using Stripe;

namespace GameServerHosting.Infrastructure.Consumers;

/// <summary>
/// Consumer for PaymentAuthorized events
/// Handles payment capture after authorization
/// </summary>
public class PaymentAuthorizedConsumer : IConsumer<PaymentAuthorized>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPaymentService _paymentService;
    private readonly IOutboxService _outboxService;
    private readonly ILogger<PaymentAuthorizedConsumer> _logger;

    public PaymentAuthorizedConsumer(
        IUnitOfWork unitOfWork,
        IPaymentService paymentService,
        IOutboxService outboxService,
        ILogger<PaymentAuthorizedConsumer> logger)
    {
        _unitOfWork = unitOfWork;
        _paymentService = paymentService;
        _outboxService = outboxService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentAuthorized> context)
    {
        var message = context.Message;
        
        _logger.LogInformation(
            "Processing PaymentAuthorized for Payment {PaymentId}, Order {OrderId}, Correlation {CorrelationId}",
            message.PaymentId, message.OrderId, message.CorrelationId);

        try
        {
            // Get the payment entity
            var payment = await _unitOfWork.Payments.GetByIdAsync(message.PaymentId);
            if (payment == null)
            {
                _logger.LogError("Payment {PaymentId} not found", message.PaymentId);
                throw new InvalidOperationException($"Payment {message.PaymentId} not found");
            }

            // Capture the payment (for payment intents that require manual capture)
            // Note: Most Stripe checkout sessions auto-capture, so this is for manual capture scenarios
            try
            {
                var paymentIntent = await _paymentService.CapturePaymentAsync(message.PaymentIntentId);

                // Update payment status
                payment.Status = PaymentStatus.Completed;
                payment.CompletedAt = DateTime.UtcNow;
                payment.UpdatedAt = DateTime.UtcNow;

                // Update order status
                var order = await _unitOfWork.Orders.GetByIdAsync(message.OrderId);
                if (order != null)
                {
                    order.Status = OrderStatus.Completed;
                    order.PaymentDate = DateTime.UtcNow;
                    order.PaymentTransactionId = paymentIntent.Id;
                    order.UpdatedAt = DateTime.UtcNow;
                }

                // Enqueue success event
                await _outboxService.EnqueueAsync(new PaymentCaptured
                {
                    CorrelationId = message.CorrelationId,
                    CausationId = message.EventId,
                    UserId = message.UserId,
                    PaymentId = message.PaymentId,
                    OrderId = message.OrderId,
                    PaymentIntentId = message.PaymentIntentId,
                    Amount = message.Amount,
                    Currency = message.Currency,
                    TransactionId = paymentIntent.Id,
                    CapturedAt = DateTime.UtcNow
                });

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation(
                    "Successfully captured payment {PaymentId} for {Amount} {Currency}",
                    message.PaymentId, message.Amount / 100.0, message.Currency.ToUpper());
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe error capturing payment {PaymentId}", message.PaymentId);

                // Update payment status to failed
                payment.Status = PaymentStatus.Failed;
                payment.ErrorMessage = ex.Message;
                payment.ErrorCode = ex.StripeError?.Code;
                payment.UpdatedAt = DateTime.UtcNow;

                // Update order status
                var order = await _unitOfWork.Orders.GetByIdAsync(message.OrderId);
                if (order != null)
                {
                    order.Status = OrderStatus.Failed;
                    order.UpdatedAt = DateTime.UtcNow;
                }

                // Enqueue failure event
                await _outboxService.EnqueueAsync(new PaymentFailed
                {
                    CorrelationId = message.CorrelationId,
                    CausationId = message.EventId,
                    UserId = message.UserId,
                    PaymentId = message.PaymentId,
                    OrderId = message.OrderId,
                    PaymentIntentId = message.PaymentIntentId,
                    Amount = message.Amount,
                    Currency = message.Currency,
                    ErrorMessage = ex.Message,
                    ErrorCode = ex.StripeError?.Code,
                    IsRetryable = ex.StripeError?.Type == "card_error" ? false : true
                });

                await _unitOfWork.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PaymentAuthorized for Payment {PaymentId}", message.PaymentId);
            throw; // Rethrow to trigger retry mechanism
        }
    }
}
