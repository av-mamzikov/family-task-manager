using FamilyTaskManager.Core.PetAggregate;

namespace FamilyTaskManager.Infrastructure.Data.Config;

public class PetConfiguration : IEntityTypeConfiguration<Pet>
{
  public void Configure(EntityTypeBuilder<Pet> builder)
  {
    builder.ToTable("Pets");

    builder.HasKey(p => p.Id);

    builder.Property(p => p.FamilyId)
      .IsRequired();

    builder.Property(p => p.Type)
      .IsRequired()
      .HasConversion<string>()
      .HasMaxLength(20);

    builder.Property(p => p.Name)
      .IsRequired()
      .HasMaxLength(100);

    builder.Property(p => p.MoodScore)
      .IsRequired()
      .HasDefaultValue(50);

    builder.Property(p => p.CreatedAt)
      .IsRequired();

    // Index for FamilyId to optimize queries
    builder.HasIndex(p => p.FamilyId);
  }
}
