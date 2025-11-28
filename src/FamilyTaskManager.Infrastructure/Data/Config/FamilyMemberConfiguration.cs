using FamilyTaskManager.Core.FamilyAggregate;

namespace FamilyTaskManager.Infrastructure.Data.Config;

public class FamilyMemberConfiguration : IEntityTypeConfiguration<FamilyMember>
{
  public void Configure(EntityTypeBuilder<FamilyMember> builder)
  {
    builder.ToTable("FamilyMembers");

    builder.HasKey(m => m.Id);

    builder.Property(m => m.UserId)
      .IsRequired();

    builder.Property(m => m.FamilyId)
      .IsRequired();

    builder.Property(m => m.Role)
      .IsRequired()
      .HasConversion<string>()
      .HasMaxLength(20);

    builder.Property(m => m.Points)
      .IsRequired()
      .HasDefaultValue(0);

    builder.Property(m => m.JoinedAt)
      .IsRequired();

    builder.Property(m => m.IsActive)
      .IsRequired()
      .HasDefaultValue(true);

    // Composite index for UserId + FamilyId
    builder.HasIndex(m => new { m.UserId, m.FamilyId });

    // Index for FamilyId to optimize queries
    builder.HasIndex(m => m.FamilyId);

    // Foreign key relationship to User
    builder.HasOne(m => m.User)
      .WithMany()
      .HasForeignKey(m => m.UserId)
      .OnDelete(DeleteBehavior.NoAction);
  }
}
