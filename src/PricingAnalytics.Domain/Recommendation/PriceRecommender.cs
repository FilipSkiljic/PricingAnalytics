using PricingAnalytics.Domain.Entities;
using PricingAnalytics.Domain.Matching;
using PricingAnalytics.Domain.Statistics;

namespace PricingAnalytics.Domain.Recommendation;

/// <summary>
/// Ties normalization, canonical-service matching and robust statistics together:
/// given a market corpus, a service name and a candidate price, tells the caller
/// whether that price is too low, fair or too high relative to the market.
/// Pure domain service - no I/O, the corpus is handed in by the Application layer.
/// </summary>
public static class PriceRecommender
{
    /// <summary>
    /// Below this many raw market entries, quartile statistics are unreliable.
    /// Exposed for callers (e.g. the UI) to show a low-confidence indicator - the
    /// engine still computes a best-effort recommendation regardless.
    /// </summary>
    public const int MinimumReliableSampleSize = 5;

    /// <param name="catalog">
    /// Canonical services to match against. Supplied by the caller rather than read from a
    /// fixed list, so the catalog can be edited at runtime without touching this code.
    /// </param>
    public static PriceRecommendation Recommend(
        IEnumerable<MarketEntry> corpus,
        string queryServiceName,
        decimal userPrice,
        IReadOnlyList<CanonicalService> catalog,
        decimal iqrOutlierFactor = 1.5m)
    {
        ArgumentNullException.ThrowIfNull(corpus);
        ArgumentNullException.ThrowIfNull(queryServiceName);
        ArgumentNullException.ThrowIfNull(catalog);

        var matchedService = ServiceMatcher.Match(queryServiceName, catalog)
            ?? throw new UnknownServiceException(queryServiceName);

        var rawPrices = corpus
            .Where(entry => ServiceMatcher.Match(entry.Name, catalog)?.Slug == matchedService.Slug)
            .Select(entry => entry.Price)
            .ToList();

        if (rawPrices.Count == 0)
        {
            throw new InsufficientMarketDataException(matchedService.DisplayName, 0);
        }

        var cleanPrices = OutlierFilter.RemoveIqrOutliers(rawPrices, iqrOutlierFactor);
        if (cleanPrices.Count == 0)
        {
            // Tiny/degenerate sample where every value looked like an outlier of the others -
            // fall back to the raw sample rather than reporting no data at all.
            cleanPrices = rawPrices;
        }

        var (q1, median, q3) = DescriptiveStatistics.Quartiles(cleanPrices);
        var percentileRank = DescriptiveStatistics.PercentileRank(cleanPrices, userPrice);
        var verdict = userPrice < q1 ? PriceVerdict.TooLow
            : userPrice > q3 ? PriceVerdict.TooHigh
            : PriceVerdict.Fair;

        return new PriceRecommendation(
            matchedService.Slug,
            matchedService.DisplayName,
            cleanPrices.Count,
            rawPrices.Count,
            median,
            q1,
            q3,
            userPrice,
            percentileRank,
            verdict,
            PriceHistogram.Build(cleanPrices));
    }
}
