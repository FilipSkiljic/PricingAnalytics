using Microsoft.EntityFrameworkCore;
using PricingAnalytics.Application.Abstractions;
using PricingAnalytics.Domain.Matching;

namespace PricingAnalytics.Infrastructure.Persistence;

public sealed class EfCanonicalServiceRepository(AppDbContext dbContext) : ICanonicalServiceRepository
{
    public async Task<IReadOnlyList<CanonicalService>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.CanonicalServices.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<CanonicalService?> FindAsync(int id, CancellationToken cancellationToken = default)
    {
        return await dbContext.CanonicalServices.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await dbContext.CanonicalServices.AnyAsync(s => s.Slug == slug, cancellationToken);
    }

    public async Task AddAsync(CanonicalService service, CancellationToken cancellationToken = default)
    {
        await dbContext.CanonicalServices.AddAsync(service, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<CanonicalService> services, CancellationToken cancellationToken = default)
    {
        await dbContext.CanonicalServices.AddRangeAsync(services, cancellationToken);
    }

    public Task RemoveAsync(CanonicalService service, CancellationToken cancellationToken = default)
    {
        dbContext.CanonicalServices.Remove(service);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
