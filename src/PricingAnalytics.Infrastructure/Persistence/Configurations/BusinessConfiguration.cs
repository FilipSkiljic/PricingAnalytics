using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PricingAnalytics.Domain.Entities;

namespace PricingAnalytics.Infrastructure.Persistence.Configurations;

public sealed class BusinessConfiguration : IEntityTypeConfiguration<Business>
{
    public void Configure(EntityTypeBuilder<Business> builder)
    {
        builder.ToTable("Businesses");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Email).IsRequired().HasMaxLength(256);
        builder.Property(b => b.PasswordHash).IsRequired().HasMaxLength(256);
        builder.Property(b => b.Name).IsRequired().HasMaxLength(200);

        // Stored as text so the value is readable when inspecting the table by hand -
        // promoting the first administrator is done directly in the database.
        builder.Property(b => b.Role)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        // One account per e-mail address - enforced by the database, not just by the
        // application check, so a race between two concurrent registrations cannot
        // create duplicates.
        builder.HasIndex(b => b.Email).IsUnique();

        builder.HasMany(b => b.Services)
            .WithOne()
            .HasForeignKey(s => s.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(Business.Services))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
