namespace GameServerHosting.Application.Interfaces;

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

    /// <summary>
    /// Sends an order confirmation email to the specified user
    /// </summary>
    /// <param name="email">Recipient email address</param>
    /// <param name="userName">User's name</param>
    /// <param name="orderNumber">Order number</param>
    /// <param name="orderDetails">Order details for the email</param>
    /// <returns>Task representing the async operation</returns>
    Task SendOrderConfirmationEmailAsync(string email, string userName, string orderNumber, OrderConfirmationDetails orderDetails);

    /// <summary>
    /// Sends a generic email with the specified subject and content
    /// </summary>
    /// <param name="toEmail">Recipient email address</param>
    /// <param name="subject">Email subject</param>
    /// <param name="htmlContent">HTML email content</param>
    /// <param name="plainTextContent">Plain text email content (optional)</param>
    /// <returns>Task representing the async operation</returns>
    Task SendEmailAsync(string toEmail, string subject, string htmlContent, string? plainTextContent = null);
}

/// <summary>
/// Details for order confirmation email
/// </summary>
public class OrderConfirmationDetails
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime OrderDate { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PaymentIdentifier { get; set; }
    public List<OrderItemDetail> Items { get; set; } = new();
    public string? BillingAddress { get; set; }
}

/// <summary>
/// Individual order item details
/// </summary>
public class OrderItemDetail
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Total { get; set; }
}
