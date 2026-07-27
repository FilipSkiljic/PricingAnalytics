using PricingAnalytics.Domain.Statistics;

namespace PricingAnalytics.Domain.Recommendation;

/// <param name="SampleSize">Market entries used for the stats, after outlier removal.</param>
/// <param name="SampleSizeBeforeOutlierRemoval">Market entries matched to the service, before outlier removal.</param>
/// <param name="Q1">Recommended range lower bound.</param>
/// <param name="Q3">Recommended range upper bound.</param>
/// <param name="PercentileRank">Where the user's price sits within the market corpus, 0-100.</param>
/// <param name="Distribution">Binned price distribution of the cleaned sample, for charting.</param>
public sealed record PriceRecommendation(
    string CanonicalServiceId,
    string CanonicalServiceName,
    int SampleSize,
    int SampleSizeBeforeOutlierRemoval,
    decimal Median,
    decimal Q1,
    decimal Q3,
    decimal UserPrice,
    double PercentileRank,
    PriceVerdict Verdict,
    IReadOnlyList<HistogramBin> Distribution);
