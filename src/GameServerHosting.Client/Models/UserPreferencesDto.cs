namespace GameServerHosting.Client.Models;

public class UserPreferencesDto
{
    // Email Preferences
    public bool EmailPromotional { get; set; }
    public bool EmailOrderUpdates { get; set; }
    public bool EmailServerAlerts { get; set; }
    public bool EmailNewsletter { get; set; }

    // Notification Preferences
    public bool NotificationsBrowser { get; set; }
    public bool NotificationsServerOffline { get; set; }
    public bool NotificationsResourceAlerts { get; set; }
    public bool NotificationsPaymentDue { get; set; }
    public bool NotificationsSecurityAlerts { get; set; }
}
