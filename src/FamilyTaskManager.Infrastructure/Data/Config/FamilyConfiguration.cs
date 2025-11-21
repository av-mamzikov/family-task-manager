using FamilyTaskManager.Core.FamilyAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyTaskManager.Infrastructure.Data.Config;

public class FamilyConfiguration : IEntityTypeConfiguration<Family>
{
  public void Configure(EntityTypeBuilder<Family> builder)
  {
    builder.ToTable("Families");

    builder.HasKey(f => f.Id);

    builder.Property(f => f.Name)
      .IsRequired()
      .HasMaxLength(100);

    builder.Property(f => f.Timezone)
      .IsRequired()
      .HasMaxLength(50)
      .HasDefaultValue("UTC");

    builder.Property(f => f.LeaderboardEnabled)
      .IsRequired()
      .HasDefaultValue(true);

    builder.Property(f => f.CreatedAt)
      .IsRequired();

    // Configure owned collection of members
    builder.HasMany(f => f.Members)
      .WithOne()
      .HasForeignKey(m => m.FamilyId)
      .OnDelete(DeleteBehavior.Cascade);
  }
}
