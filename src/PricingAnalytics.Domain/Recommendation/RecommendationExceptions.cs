namespace PricingAnalytics.Domain.Recommendation;

/// <summary>The query service name did not match any canonical service in the catalog.</summary>
public sealed class UnknownServiceException(string queryServiceName)
    : Exception($"Service '{queryServiceName}' did not match any known canonical service.")
{
    public string QueryServiceName { get; } = queryServiceName;
}

/// <summary>A canonical service was recognized, but no market entries exist for it.</summary>
public sealed class InsufficientMarketDataException(string canonicalServiceName, int sampleSize)
    : Exception($"Canonical service '{canonicalServiceName}' has {sampleSize} market entries - not enough to recommend a price.")
{
    public string CanonicalServiceName { get; } = canonicalServiceName;
    public int SampleSize { get; } = sampleSize;
}
