using PricingAnalytics.Domain.Entities;

namespace PricingAnalytics.Application.Abstractions;

/// <summary>
/// Port for the market corpus. Implemented in Infrastructure (EF Core);
/// the Application layer only depends on this abstraction, never on EF Core directly.
/// </summary>
public interface IMarketEntryRepository
{
    Task<IReadOnlyList<MarketEntry>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<MarketEntry> entries, CancellationToken cancellationToken = default);

    /// <summary>Empties the corpus - used when an import replaces it wholesale.</summary>
    Task RemoveAllAsync(CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
