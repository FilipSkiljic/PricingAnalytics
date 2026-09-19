using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PricingAnalytics.Domain.Matching;

namespace PricingAnalytics.Infrastructure.Persistence.Configurations;

public sealed class CanonicalServiceConfiguration : IEntityTypeConfiguration<CanonicalService>
{
    public void Configure(EntityTypeBuilder<CanonicalService> builder)
    {
        builder.ToTable("CanonicalServices");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Slug).IsRequired().HasMaxLength(100);
        builder.Property(s => s.DisplayName).IsRequired().HasMaxLength(200);
        builder.Property(s => s.RequiredPhrase).IsRequired().HasMaxLength(300);
        builder.Property(s => s.ExcludedWords).IsRequired().HasMaxLength(500);

        builder.HasIndex(s => s.Slug).IsUnique();

        // Derived from the phrases and cached in backing fields; nothing to persist.
        builder.Ignore(s => s.RequiredTokens);
        builder.Ignore(s => s.ExcludedTokens);
        builder.Ignore("_requiredTokens");
        builder.Ignore("_excludedTokens");
    }
}
