namespace PricingAnalytics.Domain.Entities;

/// <summary>
/// A single priced service line from the market corpus (originally imported from
/// POS exports). Has identity (Id) because it is persisted - not a value object.
/// </summary>
public sealed class MarketEntry
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal Price { get; private set; }

    private MarketEntry()
    {
        // EF Core materialization.
    }

    public MarketEntry(string name, decimal price)
    {
        Name = name;
        Price = price;
    }
}
