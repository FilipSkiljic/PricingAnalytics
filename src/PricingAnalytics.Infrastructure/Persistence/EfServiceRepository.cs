using Microsoft.EntityFrameworkCore;
using PricingAnalytics.Application.Abstractions;
using PricingAnalytics.Domain.Entities;

namespace PricingAnalytics.Infrastructure.Persistence;

public sealed class EfServiceRepository(AppDbContext dbContext) : IServiceRepository
{
    public async Task<IReadOnlyList<Service>> GetForBusinessAsync(int businessId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Services
            .AsNoTracking()
            .Where(s => s.BusinessId == businessId)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Service?> FindAsync(int serviceId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Services.FirstOrDefaultAsync(s => s.Id == serviceId, cancellationToken);
    }

    public async Task AddAsync(Service service, CancellationToken cancellationToken = default)
    {
        await dbContext.Services.AddAsync(service, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<Service> services, CancellationToken cancellationToken = default)
    {
        await dbContext.Services.AddRangeAsync(services, cancellationToken);
    }

    public Task RemoveAsync(Service service, CancellationToken cancellationToken = default)
    {
        dbContext.Services.Remove(service);
        return Task.CompletedTask;
    }

    public async Task RemoveAllForBusinessAsync(int businessId, CancellationToken cancellationToken = default)
    {
        // Set-based delete: a CSV re-import can replace hundreds of rows, and loading
        // them all just to mark them deleted would be wasteful.
        await dbContext.Services
            .Where(s => s.BusinessId == businessId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
