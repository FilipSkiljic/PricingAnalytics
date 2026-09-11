using PricingAnalytics.Application.Abstractions;

namespace PricingAnalytics.Infrastructure.Csv;

/// <summary>
/// Adapts the shared CSV reader to the Application's price-list import port, so an
/// uploaded price list is parsed by exactly the same rules as the market corpus.
/// </summary>
public sealed class PriceListCsvParser : IPriceListCsvParser
{
    public IReadOnlyList<ParsedPriceLine> Parse(string csvText)
    {
        return CsvMarketEntryReader.ReadNamePricePairs(csvText)
            .Select(pair => new ParsedPriceLine(pair.Name, pair.Price))
            .ToList();
    }
}
