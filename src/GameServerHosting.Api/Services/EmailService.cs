using GameServerHosting.Api.Models;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using MailKit.Net.Smtp;
using MimeKit;

namespace GameServerHosting.Api.Services;

/// <summary>
/// Service for sending emails via SendGrid or SMTP
/// </summary>
public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly EmailSettings _emailSettings;

    public EmailService(ILogger<EmailService> logger, IOptions<EmailSettings> emailSettings)
    {
        _logger = logger;
        _emailSettings = emailSettings.Value;
    }

    /// <inheritdoc />
    public async Task SendEmailVerificationAsync(string email, string verificationToken, string userName)
    {
        var subject = _emailSettings.Templates.EmailVerificationSubject;
        var htmlContent = GenerateEmailVerificationTemplate(verificationToken, userName);
        var plainTextContent = $"Hello {userName},\n\n" +
            $"Please verify your email address by clicking the link below:\n\n" +
            $"{_emailSettings.BaseUrl}/verify-email?token={Uri.EscapeDataString(verificationToken)}&email={Uri.EscapeDataString(email)}\n\n" +
            $"This link will expire in {_emailSettings.VerificationTokenExpirationHours} hours.\n\n" +
            $"If you didn't create an account, please ignore this email.\n\n" +
            $"Best regards,\n{_emailSettings.FromName}";

        await SendEmailAsync(email, subject, htmlContent, plainTextContent);
    }

    /// <inheritdoc />
    public async Task SendPasswordResetEmailAsync(string email, string resetToken, string userName)
    {
        var subject = _emailSettings.Templates.PasswordResetSubject;
        var htmlContent = GeneratePasswordResetTemplate(resetToken, userName);
        var plainTextContent = $"Hello {userName},\n\n" +
            $"You requested to reset your password. Click the link below to reset it:\n\n" +
            $"{_emailSettings.BaseUrl}/reset-password?token={Uri.EscapeDataString(resetToken)}&email={Uri.EscapeDataString(email)}\n\n" +
            $"This link will expire in 1 hour.\n\n" +
            $"If you didn't request this, please ignore this email and your password will remain unchanged.\n\n" +
            $"Best regards,\n{_emailSettings.FromName}";

        await SendEmailAsync(email, subject, htmlContent, plainTextContent);
    }

    /// <inheritdoc />
    public async Task SendOrderConfirmationEmailAsync(string email, string userName, string orderNumber, OrderConfirmationDetails orderDetails)
    {
        var subject = $"Order Confirmation - {orderNumber}";
        var htmlContent = GenerateOrderConfirmationTemplate(userName, orderDetails);
        var plainTextContent = GenerateOrderConfirmationPlainText(userName, orderDetails);

        await SendEmailAsync(email, subject, htmlContent, plainTextContent);
    }

    /// <summary>
    /// Sends an email using the configured provider
    /// </summary>
    private async Task SendEmailAsync(string toEmail, string subject, string htmlContent, string plainTextContent)
    {
        try
        {
            if (_emailSettings.Provider.Equals("SendGrid", StringComparison.OrdinalIgnoreCase))
            {
                await SendViaSendGridAsync(toEmail, subject, htmlContent, plainTextContent);
            }
            else
            {
                await SendViaSmtpAsync(toEmail, subject, htmlContent, plainTextContent);
            }

            _logger.LogInformation("Email sent successfully to {Email} with subject: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email} with subject: {Subject}", toEmail, subject);
            throw;
        }
    }

    /// <summary>
    /// Sends email via SendGrid
    /// </summary>
    private async Task SendViaSendGridAsync(string toEmail, string subject, string htmlContent, string plainTextContent)
    {
        if (string.IsNullOrEmpty(_emailSettings.SendGrid.ApiKey))
        {
            _logger.LogWarning("SendGrid API key is not configured. Email will be logged instead of sent.");
            LogEmailContent(toEmail, subject, plainTextContent);
            return;
        }

        var client = new SendGridClient(_emailSettings.SendGrid.ApiKey);
        var from = new EmailAddress(_emailSettings.FromEmail, _emailSettings.FromName);
        var to = new EmailAddress(toEmail);
        var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
        
        var response = await client.SendEmailAsync(msg);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Body.ReadAsStringAsync();
            _logger.LogError("SendGrid API error: {StatusCode} - {Body}", response.StatusCode, body);
            throw new Exception($"Failed to send email via SendGrid: {response.StatusCode}");
        }
    }

    /// <summary>
    /// Sends email via SMTP
    /// </summary>
    private async Task SendViaSmtpAsync(string toEmail, string subject, string htmlContent, string plainTextContent)
    {
        if (string.IsNullOrEmpty(_emailSettings.Smtp.Host))
        {
            _logger.LogWarning("SMTP host is not configured. Email will be logged instead of sent.");
            LogEmailContent(toEmail, subject, plainTextContent);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
        message.To.Add(new MailboxAddress("", toEmail));
        message.Subject = subject;

        var builder = new BodyBuilder
        {
            TextBody = plainTextContent,
            HtmlBody = htmlContent
        };
        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_emailSettings.Smtp.Host, _emailSettings.Smtp.Port, _emailSettings.Smtp.EnableSsl);

        if (!string.IsNullOrEmpty(_emailSettings.Smtp.Username) && !string.IsNullOrEmpty(_emailSettings.Smtp.Password))
        {
            await client.AuthenticateAsync(_emailSettings.Smtp.Username, _emailSettings.Smtp.Password);
        }

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    /// <summary>
    /// Logs email content when email service is not configured
    /// </summary>
    private void LogEmailContent(string toEmail, string subject, string content)
    {
        _logger.LogInformation(
            "Email would be sent to {Email}\nSubject: {Subject}\nContent:\n{Content}",
            toEmail, subject, content);
    }

    /// <summary>
    /// Generates HTML email template for email verification
    /// </summary>
    private string GenerateEmailVerificationTemplate(string verificationToken, string userName)
    {
        var verificationUrl = $"{_emailSettings.BaseUrl}/verify-email?token={Uri.EscapeDataString(verificationToken)}";
        
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body {{
            font-family: Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
        }}
        .header {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 30px;
            text-align: center;
            border-radius: 10px 10px 0 0;
        }}
        .content {{
            background: #f9f9f9;
            padding: 30px;
            border-radius: 0 0 10px 10px;
        }}
        .button {{
            display: inline-block;
            padding: 12px 30px;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            text-decoration: none;
            border-radius: 5px;
            margin: 20px 0;
            font-weight: bold;
        }}
        .footer {{
            text-align: center;
            margin-top: 30px;
            font-size: 12px;
            color: #666;
        }}
        .warning {{
            background: #fff3cd;
            border-left: 4px solid #ffc107;
            padding: 10px;
            margin: 20px 0;
        }}
    </style>
</head>
<body>
    <div class=""header"">
        <h1>🎮 Game Server Hosting</h1>
        <p>Verify Your Email Address</p>
    </div>
    <div class=""content"">
        <h2>Hello {userName}!</h2>
        <p>Thank you for registering with Game Server Hosting. We're excited to have you on board!</p>
        <p>To complete your registration and start hosting game servers, please verify your email address by clicking the button below:</p>
        
        <div style=""text-align: center;"">
            <a href=""{verificationUrl}"" class=""button"">Verify Email Address</a>
        </div>
        
        <p>Or copy and paste this link into your browser:</p>
        <p style=""word-break: break-all; background: white; padding: 10px; border-radius: 5px;"">{verificationUrl}</p>
        
        <div class=""warning"">
            <strong>⏰ Important:</strong> This verification link will expire in {_emailSettings.VerificationTokenExpirationHours} hours.
        </div>
        
        <p>If you didn't create an account with Game Server Hosting, please ignore this email.</p>
    </div>
    <div class=""footer"">
        <p>&copy; {DateTime.UtcNow.Year} Game Server Hosting. All rights reserved.</p>
        <p>This is an automated message, please do not reply to this email.</p>
    </div>
</body>
</html>";
    }

    /// <summary>
    /// Generates HTML email template for password reset
    /// </summary>
    private string GeneratePasswordResetTemplate(string resetToken, string userName)
    {
        var resetUrl = $"{_emailSettings.BaseUrl}/reset-password?token={Uri.EscapeDataString(resetToken)}";
        
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body {{
            font-family: Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
        }}
        .header {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 30px;
            text-align: center;
            border-radius: 10px 10px 0 0;
        }}
        .content {{
            background: #f9f9f9;
            padding: 30px;
            border-radius: 0 0 10px 10px;
        }}
        .button {{
            display: inline-block;
            padding: 12px 30px;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            text-decoration: none;
            border-radius: 5px;
            margin: 20px 0;
            font-weight: bold;
        }}
        .footer {{
            text-align: center;
            margin-top: 30px;
            font-size: 12px;
            color: #666;
        }}
        .warning {{
            background: #fff3cd;
            border-left: 4px solid #ffc107;
            padding: 10px;
            margin: 20px 0;
        }}
        .security {{
            background: #d1ecf1;
            border-left: 4px solid #0c5460;
            padding: 10px;
            margin: 20px 0;
        }}
    </style>
</head>
<body>
    <div class=""header"">
        <h1>🎮 Game Server Hosting</h1>
        <p>Password Reset Request</p>
    </div>
    <div class=""content"">
        <h2>Hello {userName}!</h2>
        <p>We received a request to reset your password for your Game Server Hosting account.</p>
        <p>Click the button below to reset your password:</p>
        
        <div style=""text-align: center;"">
            <a href=""{resetUrl}"" class=""button"">Reset Password</a>
        </div>
        
        <p>Or copy and paste this link into your browser:</p>
        <p style=""word-break: break-all; background: white; padding: 10px; border-radius: 5px;"">{resetUrl}</p>
        
        <div class=""warning"">
            <strong>⏰ Important:</strong> This password reset link will expire in 1 hour.
        </div>
        
        <div class=""security"">
            <strong>🔒 Security Notice:</strong> If you didn't request a password reset, please ignore this email. Your password will remain unchanged.
        </div>
    </div>
    <div class=""footer"">
        <p>&copy; {DateTime.UtcNow.Year} Game Server Hosting. All rights reserved.</p>
        <p>This is an automated message, please do not reply to this email.</p>
    </div>
</body>
</html>";
    }

    /// <summary>
    /// Generates HTML email template for order confirmation
    /// </summary>
    private string GenerateOrderConfirmationTemplate(string userName, OrderConfirmationDetails orderDetails)
    {
        var itemsHtml = string.Join("", orderDetails.Items.Select(item => $@"
            <tr>
                <td style=""padding: 15px; border-bottom: 1px solid #eee;"">
                    <strong>{item.Name}</strong>
                    {(!string.IsNullOrEmpty(item.Description) ? $"<br><small style=\"color: #666;\">{item.Description}</small>" : "")}
                </td>
                <td style=""padding: 15px; border-bottom: 1px solid #eee; text-align: center;"">{item.Quantity}</td>
                <td style=""padding: 15px; border-bottom: 1px solid #eee; text-align: right;"">${item.Price:F2}</td>
                <td style=""padding: 15px; border-bottom: 1px solid #eee; text-align: right;"">${item.Total:F2}</td>
            </tr>"));

        var dashboardUrl = $"{_emailSettings.BaseUrl}/dashboard";
        var orderUrl = $"{_emailSettings.BaseUrl}/orders/{orderDetails.OrderId}";

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body {{
            font-family: Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 650px;
            margin: 0 auto;
            padding: 20px;
            background-color: #f4f4f4;
        }}
        .email-container {{
            background: white;
            border-radius: 10px;
            overflow: hidden;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
        }}
        .header {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 40px 30px;
            text-align: center;
        }}
        .header h1 {{
            margin: 0 0 10px 0;
            font-size: 32px;
        }}
        .header .checkmark {{
            font-size: 48px;
            margin-bottom: 10px;
        }}
        .content {{
            padding: 30px;
        }}
        .order-info {{
            background: #f9f9f9;
            padding: 20px;
            border-radius: 5px;
            margin: 20px 0;
        }}
        .order-info-row {{
            display: flex;
            justify-content: space-between;
            padding: 8px 0;
            border-bottom: 1px solid #eee;
        }}
        .order-info-row:last-child {{
            border-bottom: none;
        }}
        .items-table {{
            width: 100%;
            border-collapse: collapse;
            margin: 20px 0;
            background: white;
        }}
        .items-table th {{
            background: #f9f9f9;
            padding: 12px 15px;
            text-align: left;
            font-weight: bold;
            border-bottom: 2px solid #ddd;
        }}
        .items-table th:nth-child(2),
        .items-table th:nth-child(3),
        .items-table th:nth-child(4) {{
            text-align: right;
        }}
        .total-section {{
            background: #f9f9f9;
            padding: 20px;
            border-radius: 5px;
            margin: 20px 0;
        }}
        .total-row {{
            display: flex;
            justify-content: space-between;
            padding: 8px 0;
        }}
        .total-row.final {{
            border-top: 2px solid #667eea;
            margin-top: 10px;
            padding-top: 15px;
            font-size: 18px;
            font-weight: bold;
            color: #667eea;
        }}
        .button {{
            display: inline-block;
            padding: 14px 35px;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white !important;
            text-decoration: none;
            border-radius: 5px;
            margin: 20px 10px;
            font-weight: bold;
            text-align: center;
        }}
        .button-secondary {{
            background: white;
            color: #667eea !important;
            border: 2px solid #667eea;
        }}
        .next-steps {{
            background: #e8f4fd;
            border-left: 4px solid #2196F3;
            padding: 20px;
            margin: 20px 0;
        }}
        .next-steps h3 {{
            margin-top: 0;
            color: #1976D2;
        }}
        .step {{
            padding: 10px 0;
            border-bottom: 1px solid #ccc;
        }}
        .step:last-child {{
            border-bottom: none;
        }}
        .footer {{
            background: #f9f9f9;
            padding: 30px;
            text-align: center;
            font-size: 12px;
            color: #666;
        }}
        .support-box {{
            background: #fff3cd;
            border: 1px solid #ffc107;
            padding: 15px;
            border-radius: 5px;
            margin: 20px 0;
        }}
        @media only screen and (max-width: 600px) {{
            body {{
                padding: 10px;
            }}
            .header {{
                padding: 30px 20px;
            }}
            .content {{
                padding: 20px;
            }}
            .button {{
                display: block;
                margin: 10px 0;
            }}
        }}
    </style>
</head>
<body>
    <div class=""email-container"">
        <div class=""header"">
            <div class=""checkmark"">✓</div>
            <h1>Order Confirmed!</h1>
            <p style=""font-size: 18px; margin: 10px 0 0 0;"">Thank you for your purchase</p>
        </div>
        
        <div class=""content"">
            <h2 style=""color: #333; margin-top: 0;"">Hello {userName}!</h2>
            <p>Your order has been successfully confirmed and we're now provisioning your game servers. You'll receive another notification once they're ready to use.</p>
            
            <div class=""order-info"">
                <h3 style=""margin-top: 0; color: #667eea;"">Order Information</h3>
                <div class=""order-info-row"">
                    <span><strong>Order Number:</strong></span>
                    <span>{orderDetails.OrderNumber}</span>
                </div>
                <div class=""order-info-row"">
                    <span><strong>Order Date:</strong></span>
                    <span>{orderDetails.OrderDate:MMMM dd, yyyy}</span>
                </div>
                <div class=""order-info-row"">
                    <span><strong>Payment Method:</strong></span>
                    <span>{orderDetails.PaymentMethod ?? "Credit Card"} {(!string.IsNullOrEmpty(orderDetails.PaymentIdentifier) ? orderDetails.PaymentIdentifier : "")}</span>
                </div>
            </div>

            <h3 style=""color: #333;"">Order Details</h3>
            <table class=""items-table"">
                <thead>
                    <tr>
                        <th>Item</th>
                        <th style=""text-align: center;"">Quantity</th>
                        <th style=""text-align: right;"">Price</th>
                        <th style=""text-align: right;"">Total</th>
                    </tr>
                </thead>
                <tbody>
                    {itemsHtml}
                </tbody>
            </table>

            <div class=""total-section"">
                <div class=""total-row final"">
                    <span>Total Paid:</span>
                    <span>${orderDetails.TotalAmount:F2} {orderDetails.Currency}</span>
                </div>
            </div>

            <div class=""next-steps"">
                <h3>🎮 What's Next?</h3>
                <div class=""step"">
                    <strong>1. Server Provisioning</strong>
                    <p style=""margin: 5px 0 0 0;"">Your servers are being set up and configured. This typically takes 5-10 minutes.</p>
                </div>
                <div class=""step"">
                    <strong>2. Ready Notification</strong>
                    <p style=""margin: 5px 0 0 0;"">You'll receive an email once your servers are ready with connection details.</p>
                </div>
                <div class=""step"">
                    <strong>3. Access Your Dashboard</strong>
                    <p style=""margin: 5px 0 0 0;"">Manage and monitor your servers from your dashboard.</p>
                </div>
            </div>

            <div style=""text-align: center; margin: 30px 0;"">
                <a href=""{dashboardUrl}"" class=""button"">Go to Dashboard</a>
                <a href=""{orderUrl}"" class=""button button-secondary"">View Order Details</a>
            </div>

            <div class=""support-box"">
                <strong>📞 Need Help?</strong>
                <p style=""margin: 10px 0 0 0;"">Our support team is available 24/7. Reply to this email or contact us at support@gameserverhosting.com</p>
            </div>
        </div>

        <div class=""footer"">
            <p><strong>Game Server Hosting</strong></p>
            <p>This is an automated confirmation email. Please do not reply directly to this message.</p>
            <p>&copy; {DateTime.UtcNow.Year} Game Server Hosting. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }

    /// <summary>
    /// Generates plain text version of order confirmation email
    /// </summary>
    private string GenerateOrderConfirmationPlainText(string userName, OrderConfirmationDetails orderDetails)
    {
        var itemsList = string.Join("\n", orderDetails.Items.Select(item => 
            $"  - {item.Name} x{item.Quantity} @ ${item.Price:F2} = ${item.Total:F2}"));

        return $@"
ORDER CONFIRMED!

Hello {userName}!

Thank you for your purchase. Your order has been successfully confirmed and we're now provisioning your game servers.

ORDER INFORMATION
-----------------
Order Number: {orderDetails.OrderNumber}
Order Date: {orderDetails.OrderDate:MMMM dd, yyyy}
Payment Method: {orderDetails.PaymentMethod ?? "Credit Card"} {(!string.IsNullOrEmpty(orderDetails.PaymentIdentifier) ? orderDetails.PaymentIdentifier : "")}

ORDER DETAILS
-------------
{itemsList}

Total Paid: ${orderDetails.TotalAmount:F2} {orderDetails.Currency}

WHAT'S NEXT?
------------
1. Server Provisioning
   Your servers are being set up and configured. This typically takes 5-10 minutes.

2. Ready Notification
   You'll receive an email once your servers are ready with connection details.

3. Access Your Dashboard
   Manage and monitor your servers from your dashboard at {_emailSettings.BaseUrl}/dashboard

NEED HELP?
----------
Our support team is available 24/7. Contact us at support@gameserverhosting.com

Best regards,
The Game Server Hosting Team

---
This is an automated confirmation email.
© {DateTime.UtcNow.Year} Game Server Hosting. All rights reserved.
";
    }
}
