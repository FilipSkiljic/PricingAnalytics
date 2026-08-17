using Microsoft.EntityFrameworkCore;
using PricingAnalytics.Infrastructure.Persistence;

namespace PricingAnalytics.Infrastructure.Csv;

/// <summary>One-time import of the market corpus CSV into the database. No-op if already seeded.</summary>
public static class MarketEntrySeeder
{
    public static async Task SeedFromCsvAsync(AppDbContext dbContext, string csvPath, CancellationToken cancellationToken = default)
    {
        if (await dbContext.MarketEntries.AnyAsync(cancellationToken))
        {
            return;
        }

        var entries = CsvMarketEntryReader.ReadFile(csvPath);
        dbContext.MarketEntries.AddRange(entries);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
