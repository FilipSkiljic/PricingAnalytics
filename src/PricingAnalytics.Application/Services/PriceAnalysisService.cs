using PricingAnalytics.Application.Abstractions;
using PricingAnalytics.Domain.Recommendation;

namespace PricingAnalytics.Application.Services;

/// <summary>
/// Application use case: "analyze whether a service's price is fair against the market".
/// Fetches the corpus and the catalog through repository ports and delegates the actual
/// business rules to the (pure, I/O-free) Domain.PriceRecommender.
/// </summary>
/// <remarks>
/// Returns the Domain's <see cref="PriceRecommendation"/> directly rather than mapping
/// it to a separate Application DTO - it is already a plain, immutable data record with
/// no persistence or behavior leaking out, so an extra mapping layer would just be
/// ceremony for this project's size. The Api layer maps it to an HTTP response shape.
/// </remarks>
public sealed class PriceAnalysisService(
    IMarketEntryRepository marketEntryRepository,
    ICanonicalServiceRepository catalogRepository)
{
    public async Task<PriceRecommendation> AnalyzeAsync(
        string serviceName,
        decimal price,
        CancellationToken cancellationToken = default)
    {
        var corpus = await marketEntryRepository.GetAllAsync(cancellationToken);
        var catalog = await catalogRepository.GetAllAsync(cancellationToken);

        return PriceRecommender.Recommend(corpus, serviceName, price, catalog);
    }

    /// <summary>
    /// The canonical services the engine can currently analyze - lets the UI offer
    /// them as suggestions instead of making the user guess a name that will match.
    /// Read from the catalog table, so services added through the admin screen show up
    /// here without a restart.
    /// </summary>
    public async Task<IReadOnlyList<AvailableService>> GetAvailableServicesAsync(
        CancellationToken cancellationToken = default)
    {
        var catalog = await catalogRepository.GetAllAsync(cancellationToken);

        return catalog
            .Select(service => new AvailableService(service.Slug, service.DisplayName))
            .OrderBy(service => service.DisplayName)
            .ToList();
    }
}

public sealed record AvailableService(string Id, string DisplayName);
