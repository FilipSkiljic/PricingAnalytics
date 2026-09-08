namespace PricingAnalytics.Infrastructure.Security;

/// <summary>Bound from the "Jwt" section of configuration.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>Signing key. Must be at least 32 bytes for HMAC-SHA256.</summary>
    public string Key { get; init; } = string.Empty;

    public string Issuer { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    public int ExpiryMinutes { get; init; } = 120;
}
