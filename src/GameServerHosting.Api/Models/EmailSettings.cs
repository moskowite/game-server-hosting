namespace GameServerHosting.Api.Models;

/// <summary>
/// Email service configuration settings
/// </summary>
public class EmailSettings
{
    /// <summary>
    /// Email provider to use (SMTP or SendGrid)
    /// </summary>
    public string Provider { get; set; } = "SMTP";

    /// <summary>
    /// From email address
    /// </summary>
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>
    /// From display name
    /// </summary>
    public string FromName { get; set; } = string.Empty;

    /// <summary>
    /// SendGrid configuration
    /// </summary>
    public SendGridSettings SendGrid { get; set; } = new();

    /// <summary>
    /// SMTP configuration
    /// </summary>
    public SmtpSettings Smtp { get; set; } = new();

    /// <summary>
    /// Email template settings
    /// </summary>
    public TemplateSettings Templates { get; set; } = new();

    /// <summary>
    /// Email verification token expiration in hours
    /// </summary>
    public int VerificationTokenExpirationHours { get; set; } = 24;

    /// <summary>
    /// Base URL for email links
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;
}

/// <summary>
/// SendGrid specific settings
/// </summary>
public class SendGridSettings
{
    /// <summary>
    /// SendGrid API key
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
}

/// <summary>
/// SMTP specific settings
/// </summary>
public class SmtpSettings
{
    /// <summary>
    /// SMTP server host
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// SMTP server port
    /// </summary>
    public int Port { get; set; } = 587;

    /// <summary>
    /// SMTP username
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// SMTP password
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Enable SSL/TLS
    /// </summary>
    public bool EnableSsl { get; set; } = true;
}

/// <summary>
/// Email template settings
/// </summary>
public class TemplateSettings
{
    /// <summary>
    /// Email verification subject
    /// </summary>
    public string EmailVerificationSubject { get; set; } = "Verify Your Email";

    /// <summary>
    /// Password reset subject
    /// </summary>
    public string PasswordResetSubject { get; set; } = "Reset Your Password";
}
