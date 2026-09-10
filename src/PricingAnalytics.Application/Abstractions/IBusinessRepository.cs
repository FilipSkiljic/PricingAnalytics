using PricingAnalytics.Domain.Entities;

namespace PricingAnalytics.Application.Abstractions;

/// <summary>Port for reading and persisting registered businesses (accounts).</summary>
public interface IBusinessRepository
{
    Task<Business?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<Business?> FindAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Business>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Business business, CancellationToken cancellationToken = default);

    Task RemoveAsync(Business business, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
