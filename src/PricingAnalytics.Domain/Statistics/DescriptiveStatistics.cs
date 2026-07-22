namespace PricingAnalytics.Domain.Statistics;

/// <summary>
/// Order-statistic helpers (median, percentiles, IQR, MAD) used to summarize a
/// skewed price distribution without letting extreme outliers dominate, per the
/// project's methodology: anchor on median + IQR, not mean.
/// </summary>
public static class DescriptiveStatistics
{
    /// <summary>
    /// Linear-interpolation percentile (equivalent to Excel's PERCENTILE.INC / NumPy's default).
    /// </summary>
    /// <param name="p">Fraction in [0, 1], e.g. 0.5 for the median.</param>
    public static decimal Percentile(IEnumerable<decimal> values, double p)
    {
        if (p is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(p), p, "Percentile must be between 0 and 1.");
        }

        var sorted = values.OrderBy(v => v).ToList();
        if (sorted.Count == 0)
        {
            throw new InvalidOperationException("Cannot compute a percentile of an empty sequence.");
        }

        if (sorted.Count == 1)
        {
            return sorted[0];
        }

        var rank = (sorted.Count - 1) * p;
        var lowerIndex = (int)Math.Floor(rank);
        var upperIndex = (int)Math.Ceiling(rank);
        if (lowerIndex == upperIndex)
        {
            return sorted[lowerIndex];
        }

        var fraction = (decimal)(rank - lowerIndex);
        return sorted[lowerIndex] + (sorted[upperIndex] - sorted[lowerIndex]) * fraction;
    }

    public static decimal Median(IEnumerable<decimal> values) => Percentile(values, 0.5);

    public static (decimal Q1, decimal Median, decimal Q3) Quartiles(IEnumerable<decimal> values)
    {
        var list = values.ToList();
        return (Percentile(list, 0.25), Percentile(list, 0.5), Percentile(list, 0.75));
    }

    public static decimal InterquartileRange(decimal q1, decimal q3) => q3 - q1;

    /// <summary>Median Absolute Deviation: median of |value - median|. Robust spread measure.</summary>
    public static decimal MedianAbsoluteDeviation(IEnumerable<decimal> values)
    {
        var list = values.ToList();
        var median = Median(list);
        var deviations = list.Select(v => Math.Abs(v - median));
        return Median(deviations);
    }

    /// <summary>
    /// Percentile rank of <paramref name="value"/> within the dataset (0-100): the
    /// share of the corpus at or below it, with ties counted as half-below/half-above.
    /// </summary>
    public static double PercentileRank(IEnumerable<decimal> values, decimal value)
    {
        var list = values.ToList();
        if (list.Count == 0)
        {
            throw new InvalidOperationException("Cannot compute a percentile rank against an empty sequence.");
        }

        var below = list.Count(v => v < value);
        var equal = list.Count(v => v == value);
        return (below + 0.5 * equal) / list.Count * 100.0;
    }
}
