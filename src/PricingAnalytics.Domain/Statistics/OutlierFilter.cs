namespace PricingAnalytics.Domain.Statistics;

/// <summary>
/// Removes extreme prices before they can distort the recommendation (e.g. a 230 €
/// men's haircut that is really a data-entry error, next to a genuine 1 € entry).
/// Both methods are order-statistic based, matching the median/IQR anchoring used
/// throughout the engine - no mean/stddev involved.
/// </summary>
public static class OutlierFilter
{
    private const int MinSampleSizeToFilter = 4;

    /// <summary>Drops values outside [Q1 - k*IQR, Q3 + k*IQR]. k = 1.5 is the conventional Tukey fence.</summary>
    public static IReadOnlyList<decimal> RemoveIqrOutliers(IEnumerable<decimal> values, decimal k = 1.5m)
    {
        var list = values.ToList();
        if (list.Count < MinSampleSizeToFilter)
        {
            return list;
        }

        var (q1, _, q3) = DescriptiveStatistics.Quartiles(list);
        var iqr = DescriptiveStatistics.InterquartileRange(q1, q3);
        var lowerBound = q1 - k * iqr;
        var upperBound = q3 + k * iqr;

        return list.Where(v => v >= lowerBound && v <= upperBound).ToList();
    }

    /// <summary>
    /// Drops values whose modified z-score (0.6745 * (v - median) / MAD) exceeds k in
    /// magnitude. k = 3.5 is the commonly cited Iglewicz &amp; Hoaglin threshold.
    /// </summary>
    public static IReadOnlyList<decimal> RemoveMadOutliers(IEnumerable<decimal> values, decimal k = 3.5m)
    {
        var list = values.ToList();
        if (list.Count < MinSampleSizeToFilter)
        {
            return list;
        }

        var median = DescriptiveStatistics.Median(list);
        var mad = DescriptiveStatistics.MedianAbsoluteDeviation(list);
        if (mad == 0)
        {
            // Every value is identical (or collapses to the median after rounding) -
            // there is nothing meaningful to score deviations against.
            return list;
        }

        return list.Where(v => Math.Abs(0.6745m * (v - median) / mad) <= k).ToList();
    }
}
