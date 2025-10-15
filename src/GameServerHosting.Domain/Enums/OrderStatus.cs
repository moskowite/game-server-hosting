namespace GameServerHosting.Domain.Enums;

/// <summary>
/// Status of order processing
/// </summary>
public enum OrderStatus
{
    /// <summary>
    /// Order created, awaiting payment
    /// </summary>
    Pending,
    
    /// <summary>
    /// Payment successful, order completed
    /// </summary>
    Completed,
    
    /// <summary>
    /// Payment failed or order cancelled
    /// </summary>
    Failed,
    
    /// <summary>
    /// Order refunded to customer
    /// </summary>
    Refunded
}
