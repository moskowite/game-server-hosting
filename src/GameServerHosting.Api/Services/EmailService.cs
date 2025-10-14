namespace GameServerHosting.Api.Services;

/// <summary>
/// Service for sending emails
/// NOTE: This is a stub implementation. In production, integrate with SendGrid, AWS SES, or similar service.
/// </summary>
public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _configuration;

    public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    /// <inheritdoc />
    public async Task SendPasswordResetEmailAsync(string email, string resetToken, string userName)
    {
        // TODO: Replace with actual email service implementation
        // For now, just log the password reset token
        _logger.LogInformation(
            "Password reset email would be sent to {Email} for user {UserName}. Reset token: {Token}",
            email, userName, resetToken);

        // In a real implementation, you would:
        // 1. Create an HTML email template
        // 2. Generate a password reset URL with the token
        // 3. Send the email via your email service provider

        await Task.CompletedTask;
    }
}
