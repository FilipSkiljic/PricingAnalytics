using Microsoft.EntityFrameworkCore;
using PricingAnalytics.Application.Abstractions;
using PricingAnalytics.Domain.Entities;

namespace PricingAnalytics.Infrastructure.Persistence;

public sealed class EfBusinessRepository(AppDbContext dbContext) : IBusinessRepository
{
    public async Task<Business?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await dbContext.Businesses.FirstOrDefaultAsync(b => b.Email == email, cancellationToken);
    }

    public async Task<Business?> FindAsync(int id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Businesses.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        return await dbContext.Businesses.AnyAsync(b => b.Email == email, cancellationToken);
    }

    public async Task<IReadOnlyList<Business>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Businesses
            .AsNoTracking()
            .OrderBy(b => b.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Businesses.CountAsync(cancellationToken);
    }

    public async Task AddAsync(Business business, CancellationToken cancellationToken = default)
    {
        await dbContext.Businesses.AddAsync(business, cancellationToken);
    }

    public Task RemoveAsync(Business business, CancellationToken cancellationToken = default)
    {
        dbContext.Businesses.Remove(business);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
