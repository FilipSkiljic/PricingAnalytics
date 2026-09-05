namespace PricingAnalytics.Domain.Entities;

/// <summary>
/// One line of a business's own price list - the thing that gets compared against
/// the market corpus. Kept deliberately close to <see cref="MarketEntry"/> in shape,
/// since both originate from the same kind of POS export.
/// </summary>
public sealed class Service
{
    public int Id { get; private set; }
    public int BusinessId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal Price { get; private set; }

    private Service()
    {
        // EF Core materialization.
    }

    public Service(int businessId, string name, decimal price)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegative(price);

        BusinessId = businessId;
        Name = name.Trim();
        Price = price;
    }

    public void UpdatePrice(decimal price)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(price);
        Price = price;
    }
}
