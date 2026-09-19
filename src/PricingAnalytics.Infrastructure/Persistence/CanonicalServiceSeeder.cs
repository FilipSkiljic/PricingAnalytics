using Microsoft.EntityFrameworkCore;
using PricingAnalytics.Domain.Matching;

namespace PricingAnalytics.Infrastructure.Persistence;

/// <summary>
/// Fills an empty catalog table with the starting set of services. No-op once the table
/// holds anything, so an administrator's edits are never overwritten on restart.
/// </summary>
public static class CanonicalServiceSeeder
{
    public static async Task SeedAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (await dbContext.CanonicalServices.AnyAsync(cancellationToken))
        {
            return;
        }

        dbContext.CanonicalServices.AddRange(CanonicalServiceCatalog.SeedData());
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
