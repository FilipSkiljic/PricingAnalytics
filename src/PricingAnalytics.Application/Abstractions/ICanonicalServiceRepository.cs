using PricingAnalytics.Domain.Matching;

namespace PricingAnalytics.Application.Abstractions;

/// <summary>Port for the canonical service catalog, which is editable at runtime.</summary>
public interface ICanonicalServiceRepository
{
    Task<IReadOnlyList<CanonicalService>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<CanonicalService?> FindAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default);

    Task AddAsync(CanonicalService service, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<CanonicalService> services, CancellationToken cancellationToken = default);

    Task RemoveAsync(CanonicalService service, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
