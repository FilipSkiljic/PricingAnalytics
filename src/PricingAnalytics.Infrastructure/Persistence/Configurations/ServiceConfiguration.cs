using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PricingAnalytics.Domain.Entities;

namespace PricingAnalytics.Infrastructure.Persistence.Configurations;

public sealed class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.ToTable("Services");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).IsRequired().HasMaxLength(500);
        builder.Property(s => s.Price).HasColumnType("decimal(10,2)");

        // Every read of a price list filters by owner, so this index carries that query.
        builder.HasIndex(s => s.BusinessId);
    }
}
