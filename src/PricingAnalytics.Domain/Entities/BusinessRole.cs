namespace PricingAnalytics.Domain.Entities;

public enum BusinessRole
{
    /// <summary>Ordinary account: own price list and market analysis.</summary>
    User = 0,

    /// <summary>May additionally manage the canonical service catalog and the market corpus.</summary>
    Admin = 1,
}
