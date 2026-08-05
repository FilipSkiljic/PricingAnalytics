namespace PricingAnalytics.Domain.Statistics;

/// <param name="LowerBound">Inclusive lower edge of the bin.</param>
/// <param name="UpperBound">Upper edge of the bin (inclusive only for the last bin).</param>
/// <param name="Count">How many market prices fall inside the bin.</param>
public sealed record HistogramBin(decimal LowerBound, decimal UpperBound, int Count);

/// <summary>
/// Buckets a price sample into equal-width bins so the UI can draw the market
/// distribution. Pure domain code - the presentation layer decides how to render it.
/// </summary>
public static class PriceHistogram
{
    /// <summary>
    /// Enough bars to show the shape of the distribution (skew, clustering around
    /// round prices) without becoming noise on a small sample.
    /// </summary>
    public const int DefaultBinCount = 12;

    public static IReadOnlyList<HistogramBin> Build(IEnumerable<decimal> values, int binCount = DefaultBinCount)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentOutOfRangeException.ThrowIfLessThan(binCount, 1);

        var list = values.ToList();
        if (list.Count == 0)
        {
            return Array.Empty<HistogramBin>();
        }

        var min = list.Min();
        var max = list.Max();

        if (min == max)
        {
            // Degenerate sample - every price is identical, so a single bin describes it fully.
            return new[] { new HistogramBin(min, max, list.Count) };
        }

        var binWidth = (max - min) / binCount;
        var counts = new int[binCount];

        foreach (var value in list)
        {
            // The last bin is closed on the right so the maximum value lands inside it
            // instead of falling one index past the end.
            var index = Math.Min((int)((value - min) / binWidth), binCount - 1);
            counts[index]++;
        }

        return Enumerable.Range(0, binCount)
            .Select(i => new HistogramBin(
                min + binWidth * i,
                min + binWidth * (i + 1),
                counts[i]))
            .ToList();
    }
}
