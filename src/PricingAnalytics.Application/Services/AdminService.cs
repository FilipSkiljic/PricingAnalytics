using PricingAnalytics.Application.Abstractions;
using PricingAnalytics.Domain.Entities;
using PricingAnalytics.Domain.Matching;

namespace PricingAnalytics.Application.Services;

public sealed record BusinessView(int Id, string Name, string Email, string Role, int ServiceCount);

/// <param name="CoveragePercent">Share of the corpus the catalog currently claims, 0-100.</param>
public sealed record SystemStatistics(
    int RegisteredBusinesses,
    int CorpusEntries,
    int CatalogServices,
    int MatchedEntries,
    double CoveragePercent);

public sealed record CorpusImportResult(int Imported);

public sealed class AdminOperationException(string message) : Exception(message);

/// <summary>
/// Administration of the system itself: who is registered, how big the corpus is, how much
/// of it the catalog reaches, and replacing the corpus from a fresh export.
/// </summary>
public sealed class AdminService(
    IBusinessRepository businessRepository,
    IServiceRepository serviceRepository,
    IMarketEntryRepository marketEntryRepository,
    ICanonicalServiceRepository catalogRepository,
    IPriceListCsvParser csvParser)
{
    public async Task<IReadOnlyList<BusinessView>> GetBusinessesAsync(CancellationToken cancellationToken = default)
    {
        var businesses = await businessRepository.GetAllAsync(cancellationToken);
        var views = new List<BusinessView>(businesses.Count);

        foreach (var business in businesses)
        {
            var services = await serviceRepository.GetForBusinessAsync(business.Id, cancellationToken);
            views.Add(new BusinessView(
                business.Id, business.Name, business.Email, business.Role.ToString(), services.Count));
        }

        return views;
    }

    /// <summary>
    /// Deletes an account and, by cascade, its price list.
    /// <paramref name="requestingBusinessId"/> guards against an administrator deleting
    /// themselves, which would leave the system without an administrator.
    /// </summary>
    public async Task DeleteBusinessAsync(
        int businessId,
        int requestingBusinessId,
        CancellationToken cancellationToken = default)
    {
        if (businessId == requestingBusinessId)
        {
            throw new AdminOperationException("Ne možete obrisati vlastiti račun.");
        }

        var business = await businessRepository.FindAsync(businessId, cancellationToken)
            ?? throw new AdminOperationException("Račun nije pronađen.");

        await businessRepository.RemoveAsync(business, cancellationToken);
        await businessRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<SystemStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var businesses = await businessRepository.CountAsync(cancellationToken);
        var corpus = await marketEntryRepository.GetAllAsync(cancellationToken);
        var catalog = await catalogRepository.GetAllAsync(cancellationToken);

        var matched = corpus.Count(entry => ServiceMatcher.Match(entry.Name, catalog) is not null);
        var coverage = corpus.Count == 0 ? 0 : (double)matched / corpus.Count * 100;

        return new SystemStatistics(businesses, corpus.Count, catalog.Count, matched, coverage);
    }

    /// <summary>
    /// Replaces the whole market corpus from an uploaded export. Replace rather than append:
    /// a corpus assembled from two different exports would double-count the same businesses.
    /// </summary>
    public async Task<CorpusImportResult> ImportCorpusAsync(
        string csvText,
        CancellationToken cancellationToken = default)
    {
        var lines = csvParser.Parse(csvText);
        if (lines.Count == 0)
        {
            throw new AdminOperationException(
                "U datoteci nije pronađen nijedan valjan redak. Očekuju se stupci \"Name\" i \"Price\".");
        }

        var entries = lines.Select(line => new MarketEntry(line.Name, line.Price)).ToList();

        await marketEntryRepository.RemoveAllAsync(cancellationToken);
        await marketEntryRepository.AddRangeAsync(entries, cancellationToken);
        await marketEntryRepository.SaveChangesAsync(cancellationToken);

        return new CorpusImportResult(entries.Count);
    }
}
