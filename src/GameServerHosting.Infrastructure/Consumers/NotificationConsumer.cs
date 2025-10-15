using GameServerHosting.Application.Interfaces;
using GameServerHosting.Shared.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace GameServerHosting.Infrastructure.Consumers;

/// <summary>
/// Consumer for lifecycle events to send notifications to users
/// Handles email notifications, webhooks, and SignalR updates
/// </summary>
public class NotificationConsumer : 
    IConsumer<ServerProvisioned>,
    IConsumer<ServerProvisioningFailed>,
    IConsumer<PaymentCaptured>,
    IConsumer<PaymentFailed>,
    IConsumer<ServerHealthCheckFailed>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly ILogger<NotificationConsumer> _logger;

    public NotificationConsumer(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        ILogger<NotificationConsumer> logger)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ServerProvisioned> context)
    {
        var message = context.Message;
        
        _logger.LogInformation(
            "Sending notification for ServerProvisioned - GameServer {GameServerId}",
            message.GameServerId);

        try
        {
            // Get user details
            if (message.UserId.HasValue)
            {
                var user = await _unitOfWork.Users.GetByIdAsync(message.UserId.Value);
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    // Send email notification
                    var subject = "Your Game Server is Ready!";
                    var body = $@"
                        <h2>Great news!</h2>
                        <p>Your game server has been successfully provisioned and is now ready to use.</p>
                        <p><strong>Server Details:</strong></p>
                        <ul>
                            <li>IP Address: {message.IpAddress}</li>
                            <li>Port: {message.Port}</li>
                            <li>Region: {message.Region}</li>
                        </p>
                        <p>You can now connect to your server using the details above.</p>
                    ";

                    await _emailService.SendEmailAsync(user.Email, subject, body);
                    _logger.LogInformation("Sent provisioning success email to {Email}", user.Email);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification for ServerProvisioned {GameServerId}", message.GameServerId);
            // Don't rethrow - notifications are best-effort
        }
    }

    public async Task Consume(ConsumeContext<ServerProvisioningFailed> context)
    {
        var message = context.Message;
        
        _logger.LogInformation(
            "Sending notification for ServerProvisioningFailed - GameServer {GameServerId}",
            message.GameServerId);

        try
        {
            if (message.UserId.HasValue)
            {
                var user = await _unitOfWork.Users.GetByIdAsync(message.UserId.Value);
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    var subject = "Server Provisioning Failed";
                    var body = $@"
                        <h2>Server Provisioning Issue</h2>
                        <p>Unfortunately, we encountered an issue provisioning your game server.</p>
                        <p><strong>Error:</strong> {message.ErrorMessage}</p>
                        <p>Our team has been notified and will look into this issue. We'll contact you shortly with an update.</p>
                        <p>If you have any questions, please don't hesitate to contact our support team.</p>
                    ";

                    await _emailService.SendEmailAsync(user.Email, subject, body);
                    _logger.LogInformation("Sent provisioning failure email to {Email}", user.Email);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification for ServerProvisioningFailed {GameServerId}", message.GameServerId);
        }
    }

    public async Task Consume(ConsumeContext<PaymentCaptured> context)
    {
        var message = context.Message;
        
        _logger.LogInformation(
            "Sending notification for PaymentCaptured - Payment {PaymentId}",
            message.PaymentId);

        try
        {
            if (message.UserId.HasValue)
            {
                var user = await _unitOfWork.Users.GetByIdAsync(message.UserId.Value);
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    var subject = "Payment Confirmed";
                    var body = $@"
                        <h2>Payment Successful</h2>
                        <p>Your payment has been successfully processed.</p>
                        <p><strong>Payment Details:</strong></p>
                        <ul>
                            <li>Amount: {message.Amount / 100.0:C} {message.Currency.ToUpper()}</li>
                            <li>Transaction ID: {message.TransactionId}</li>
                            <li>Date: {message.CapturedAt:f}</li>
                        </ul>
                        <p>Thank you for your business!</p>
                    ";

                    await _emailService.SendEmailAsync(user.Email, subject, body);
                    _logger.LogInformation("Sent payment confirmation email to {Email}", user.Email);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification for PaymentCaptured {PaymentId}", message.PaymentId);
        }
    }

    public async Task Consume(ConsumeContext<PaymentFailed> context)
    {
        var message = context.Message;
        
        _logger.LogInformation(
            "Sending notification for PaymentFailed - Payment {PaymentId}",
            message.PaymentId);

        try
        {
            if (message.UserId.HasValue)
            {
                var user = await _unitOfWork.Users.GetByIdAsync(message.UserId.Value);
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    var subject = "Payment Failed";
                    var body = $@"
                        <h2>Payment Issue</h2>
                        <p>Unfortunately, we were unable to process your payment.</p>
                        <p><strong>Error:</strong> {message.ErrorMessage}</p>
                        <p>Please try again with a different payment method or contact your bank for assistance.</p>
                    ";

                    await _emailService.SendEmailAsync(user.Email, subject, body);
                    _logger.LogInformation("Sent payment failure email to {Email}", user.Email);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification for PaymentFailed {PaymentId}", message.PaymentId);
        }
    }

    public async Task Consume(ConsumeContext<ServerHealthCheckFailed> context)
    {
        var message = context.Message;
        
        _logger.LogInformation(
            "Sending notification for ServerHealthCheckFailed - GameServer {GameServerId}",
            message.GameServerId);

        try
        {
            if (message.UserId.HasValue)
            {
                var user = await _unitOfWork.Users.GetByIdAsync(message.UserId.Value);
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    var subject = "Server Health Alert";
                    var body = $@"
                        <h2>Server Health Alert</h2>
                        <p>Your game server at {message.IpAddress}:{message.Port} has failed a health check.</p>
                        <p><strong>Details:</strong></p>
                        <ul>
                            <li>Status: {message.ServerStatus}</li>
                            <li>Reason: {message.FailureReason}</li>
                            <li>Consecutive Failures: {message.ConsecutiveFailures}</li>
                            <li>Checked At: {message.CheckedAt:f}</li>
                        </ul>
                        <p>Please check your server status in the dashboard.</p>
                    ";

                    await _emailService.SendEmailAsync(user.Email, subject, body);
                    _logger.LogInformation("Sent health check failure email to {Email}", user.Email);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification for ServerHealthCheckFailed {GameServerId}", message.GameServerId);
        }
    }
}
