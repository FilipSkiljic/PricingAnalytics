using PricingAnalytics.Domain.Entities;

namespace PricingAnalytics.Application.Abstractions;

/// <summary>Port for a business's own price list.</summary>
public interface IServiceRepository
{
    Task<IReadOnlyList<Service>> GetForBusinessAsync(int businessId, CancellationToken cancellationToken = default);

    Task<Service?> FindAsync(int serviceId, CancellationToken cancellationToken = default);

    Task AddAsync(Service service, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<Service> services, CancellationToken cancellationToken = default);

    Task RemoveAsync(Service service, CancellationToken cancellationToken = default);

    /// <summary>Wipes a business's price list - used when a CSV import replaces it wholesale.</summary>
    Task RemoveAllForBusinessAsync(int businessId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
