using Microsoft.EntityFrameworkCore;
using PricingAnalytics.Domain.Entities;
using PricingAnalytics.Domain.Matching;

namespace PricingAnalytics.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<MarketEntry> MarketEntries => Set<MarketEntry>();
    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<CanonicalService> CanonicalServices => Set<CanonicalService>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
