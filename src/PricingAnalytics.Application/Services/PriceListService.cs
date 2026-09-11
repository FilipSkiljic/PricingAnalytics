using PricingAnalytics.Application.Abstractions;
using PricingAnalytics.Domain.Entities;
using PricingAnalytics.Domain.Matching;
using PricingAnalytics.Domain.Recommendation;

namespace PricingAnalytics.Application.Services;

/// <summary>
/// One row of a business's price list together with its market verdict.
/// <paramref name="Recommendation"/> is null when the service name does not match any
/// canonical service - the row is still shown, just without an assessment.
/// </summary>
public sealed record PriceListItem(int Id, string Name, decimal Price, PriceRecommendation? Recommendation);

/// <param name="Imported">Rows written to the price list.</param>
/// <param name="Skipped">Rows the parser could not use (missing name, unparseable price).</param>
public sealed record ImportResult(int Imported, int Skipped);

public sealed class PriceListNotFoundException(string message) : Exception(message);

/// <summary>
/// Application use cases around a business's own price list: listing it with market
/// verdicts attached, editing individual rows, and bulk-importing it from a CSV export.
/// </summary>
public sealed class PriceListService(
    IServiceRepository serviceRepository,
    IMarketEntryRepository marketEntryRepository,
    ICanonicalServiceRepository catalogRepository,
    IPriceListCsvParser csvParser)
{
    /// <summary>
    /// Returns the business's price list, each row analyzed against the market corpus.
    /// The corpus is fetched once and reused for every row rather than per service.
    /// </summary>
    public async Task<IReadOnlyList<PriceListItem>> GetAnalyzedAsync(
        int businessId,
        CancellationToken cancellationToken = default)
    {
        var services = await serviceRepository.GetForBusinessAsync(businessId, cancellationToken);
        if (services.Count == 0)
        {
            return [];
        }

        var corpus = await marketEntryRepository.GetAllAsync(cancellationToken);
        var catalog = await catalogRepository.GetAllAsync(cancellationToken);

        return services
            .Select(service => new PriceListItem(
                service.Id,
                service.Name,
                service.Price,
                TryRecommend(corpus, catalog, service.Name, service.Price)))
            .ToList();
    }

    public async Task<Service> AddAsync(
        int businessId,
        string name,
        decimal price,
        CancellationToken cancellationToken = default)
    {
        var service = new Service(businessId, name, price);
        await serviceRepository.AddAsync(service, cancellationToken);
        await serviceRepository.SaveChangesAsync(cancellationToken);
        return service;
    }

    public async Task DeleteAsync(int businessId, int serviceId, CancellationToken cancellationToken = default)
    {
        var service = await serviceRepository.FindAsync(serviceId, cancellationToken);

        // Treat "belongs to someone else" exactly like "does not exist", so the response
        // does not reveal that another business owns this id.
        if (service is null || service.BusinessId != businessId)
        {
            throw new PriceListNotFoundException("Usluga nije pronađena.");
        }

        await serviceRepository.RemoveAsync(service, cancellationToken);
        await serviceRepository.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Replaces the business's price list with the contents of an uploaded CSV.
    /// Replace rather than merge: re-uploading a corrected export is the common case,
    /// and merging would silently accumulate duplicates.
    /// </summary>
    public async Task<ImportResult> ImportFromCsvAsync(
        int businessId,
        string csvText,
        CancellationToken cancellationToken = default)
    {
        var lines = csvParser.Parse(csvText);
        if (lines.Count == 0)
        {
            throw new PriceListNotFoundException(
                "U datoteci nije pronađen nijedan valjan redak. Očekuju se stupci \"Name\" i \"Price\".");
        }

        var services = new List<Service>(lines.Count);
        var skipped = 0;

        foreach (var line in lines)
        {
            try
            {
                services.Add(new Service(businessId, line.Name, line.Price));
            }
            catch (ArgumentException)
            {
                // Blank name or negative price - keep importing the rest of the file.
                skipped++;
            }
        }

        await serviceRepository.RemoveAllForBusinessAsync(businessId, cancellationToken);
        await serviceRepository.AddRangeAsync(services, cancellationToken);
        await serviceRepository.SaveChangesAsync(cancellationToken);

        return new ImportResult(services.Count, skipped);
    }

    private static PriceRecommendation? TryRecommend(
        IReadOnlyList<MarketEntry> corpus,
        IReadOnlyList<CanonicalService> catalog,
        string name,
        decimal price)
    {
        try
        {
            return PriceRecommender.Recommend(corpus, name, price, catalog);
        }
        catch (Exception exception) when (exception is UnknownServiceException or InsufficientMarketDataException)
        {
            // Not every line of a real price list has a market counterpart; that is expected.
            return null;
        }
    }
}
