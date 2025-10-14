using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameServerHosting.Shared.Entities;

/// <summary>
/// User preferences for email and notification settings
/// </summary>
public class UserPreferences
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Associated user ID
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    // Email Preferences
    /// <summary>
    /// Receive promotional emails
    /// </summary>
    public bool EmailPromotional { get; set; } = true;

    /// <summary>
    /// Receive order updates via email
    /// </summary>
    public bool EmailOrderUpdates { get; set; } = true;

    /// <summary>
    /// Receive server status alerts via email
    /// </summary>
    public bool EmailServerAlerts { get; set; } = true;

    /// <summary>
    /// Receive newsletter emails
    /// </summary>
    public bool EmailNewsletter { get; set; } = true;

    // Notification Preferences
    /// <summary>
    /// Enable browser notifications
    /// </summary>
    public bool NotificationsBrowser { get; set; } = true;

    /// <summary>
    /// Notify when server goes offline
    /// </summary>
    public bool NotificationsServerOffline { get; set; } = true;

    /// <summary>
    /// Notify when server resources are low
    /// </summary>
    public bool NotificationsResourceAlerts { get; set; } = true;

    /// <summary>
    /// Notify when payment is due
    /// </summary>
    public bool NotificationsPaymentDue { get; set; } = true;

    /// <summary>
    /// Notify about security events
    /// </summary>
    public bool NotificationsSecurityAlerts { get; set; } = true;

    /// <summary>
    /// Created timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last updated timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // Navigation property
    [ForeignKey(nameof(UserId))]
    public ApplicationUser User { get; set; } = null!;
}
