namespace PricingAnalytics.Application.Abstractions;

/// <summary>One successfully parsed "Name","Price" row of an uploaded price list.</summary>
public sealed record ParsedPriceLine(string Name, decimal Price);

/// <summary>
/// Port for turning an uploaded CSV into price lines. The parsing itself (quoting,
/// embedded newlines, culture-invariant decimals) is an Infrastructure concern.
/// </summary>
public interface IPriceListCsvParser
{
    IReadOnlyList<ParsedPriceLine> Parse(string csvText);
}
