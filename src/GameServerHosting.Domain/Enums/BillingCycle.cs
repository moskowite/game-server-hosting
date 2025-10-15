namespace GameServerHosting.Domain.Enums;

/// <summary>
/// Billing cycle options for server plans
/// </summary>
public enum BillingCycle
{
    /// <summary>
    /// Hourly billing
    /// </summary>
    Hourly,
    
    /// <summary>
    /// Daily billing (24 hours)
    /// </summary>
    Daily,
    
    /// <summary>
    /// Weekly billing (7 days)
    /// </summary>
    Weekly,
    
    /// <summary>
    /// Monthly billing (30 days)
    /// </summary>
    Monthly,
    
    /// <summary>
    /// Quarterly billing (90 days)
    /// </summary>
    Quarterly,
    
    /// <summary>
    /// Semi-annual billing (180 days)
    /// </summary>
    SemiAnnual,
    
    /// <summary>
    /// Annual billing (365 days)
    /// </summary>
    Annual
}
