using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PricingAnalytics.Domain.Entities;

namespace PricingAnalytics.Infrastructure.Persistence.Configurations;

public sealed class MarketEntryConfiguration : IEntityTypeConfiguration<MarketEntry>
{
    public void Configure(EntityTypeBuilder<MarketEntry> builder)
    {
        builder.ToTable("MarketEntries");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).IsRequired().HasMaxLength(500);
        builder.Property(e => e.Price).HasColumnType("decimal(10,2)");
    }
}
