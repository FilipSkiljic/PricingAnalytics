using Microsoft.EntityFrameworkCore;
using PricingAnalytics.Application.Abstractions;
using PricingAnalytics.Domain.Entities;

namespace PricingAnalytics.Infrastructure.Persistence;

public sealed class EfMarketEntryRepository(AppDbContext dbContext) : IMarketEntryRepository
{
    public async Task<IReadOnlyList<MarketEntry>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.MarketEntries.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.MarketEntries.CountAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<MarketEntry> entries, CancellationToken cancellationToken = default)
    {
        await dbContext.MarketEntries.AddRangeAsync(entries, cancellationToken);
    }

    public async Task RemoveAllAsync(CancellationToken cancellationToken = default)
    {
        // Set-based delete: the corpus runs to thousands of rows, and loading them all
        // just to mark them deleted would be wasteful.
        await dbContext.MarketEntries.ExecuteDeleteAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
