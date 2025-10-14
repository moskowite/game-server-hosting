namespace GameServerHosting.Api.Services;

/// <summary>
/// Service interface for sending emails
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email verification email to the specified user
    /// </summary>
    /// <param name="email">Recipient email address</param>
    /// <param name="verificationToken">Email verification token</param>
    /// <param name="userName">User's name</param>
    /// <returns>Task representing the async operation</returns>
    Task SendEmailVerificationAsync(string email, string verificationToken, string userName);

    /// <summary>
    /// Sends a password reset email to the specified user
    /// </summary>
    /// <param name="email">Recipient email address</param>
    /// <param name="resetToken">Password reset token</param>
    /// <param name="userName">User's name</param>
    /// <returns>Task representing the async operation</returns>
    Task SendPasswordResetEmailAsync(string email, string resetToken, string userName);
}
