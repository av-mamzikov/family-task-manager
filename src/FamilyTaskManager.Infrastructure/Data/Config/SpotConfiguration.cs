using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.SpotAggregate;

namespace FamilyTaskManager.Infrastructure.Data.Config;

public class SpotConfiguration : IEntityTypeConfiguration<Spot>
{
  public void Configure(EntityTypeBuilder<Spot> builder)
  {
    builder.ToTable("Spots");

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

    // Foreign key relationship to Family
    builder.HasOne(p => p.Family)
      .WithMany()
      .HasForeignKey(p => p.FamilyId)
      .OnDelete(DeleteBehavior.Cascade);

    // Many-to-many: Spot <-> responsible FamilyMembers
    builder
      .HasMany(p => p.ResponsibleMembers)
      .WithMany(m => m.ResponsibleSpots)
      .UsingEntity<Dictionary<string, object>>(
        "SpotResponsibleMembers",
        j => j
          .HasOne<FamilyMember>()
          .WithMany()
          .HasForeignKey("FamilyMemberId")
          .OnDelete(DeleteBehavior.Cascade),
        j => j
          .HasOne<Spot>()
          .WithMany()
          .HasForeignKey("SpotId")
          .OnDelete(DeleteBehavior.Cascade));

    builder
      .Navigation(p => p.ResponsibleMembers)
      .UsePropertyAccessMode(PropertyAccessMode.Field)
      .AutoInclude();
  }
}
