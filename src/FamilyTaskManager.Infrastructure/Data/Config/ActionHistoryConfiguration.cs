using FamilyTaskManager.Core.ActionHistoryAggregate;

namespace FamilyTaskManager.Infrastructure.Data.Config;

public class ActionHistoryConfiguration : IEntityTypeConfiguration<ActionHistory>
{
  public void Configure(EntityTypeBuilder<ActionHistory> builder)
  {
    builder.ToTable("ActionHistory");

    builder.HasKey(a => a.Id);

    builder.Property(a => a.FamilyId)
      .IsRequired();

    builder.Property(a => a.UserId)
      .IsRequired();

    builder.Property(a => a.ActionType)
      .IsRequired()
      .HasConversion<string>()
      .HasMaxLength(50);

    builder.Property(a => a.Description)
      .IsRequired()
      .HasMaxLength(500);

    builder.Property(a => a.MetadataJson)
      .IsRequired(false)
      .HasColumnType("jsonb");

    builder.Property(a => a.CreatedAt)
      .IsRequired();

    // Indexes for optimization
    builder.HasIndex(a => a.FamilyId);
    builder.HasIndex(a => new { a.FamilyId, a.CreatedAt });
    builder.HasIndex(a => new { a.FamilyId, a.UserId, a.CreatedAt });

    // Foreign key relationship to Family
    builder.HasOne(a => a.Family)
      .WithMany()
      .HasForeignKey(a => a.FamilyId)
      .OnDelete(DeleteBehavior.Cascade);

    // Foreign key relationship to User
    builder.HasOne(a => a.User)
      .WithMany()
      .HasForeignKey(a => a.UserId)
      .OnDelete(DeleteBehavior.NoAction);
  }
}
