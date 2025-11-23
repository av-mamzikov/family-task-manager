using FamilyTaskManager.Core.FamilyAggregate;

namespace FamilyTaskManager.Infrastructure.Data.Config;

public class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
  public void Configure(EntityTypeBuilder<Invitation> builder)
  {
    builder.ToTable("Invitations");

    builder.HasKey(i => i.Id);

    builder.Property(i => i.FamilyId)
      .IsRequired();

    builder.Property(i => i.Role)
      .IsRequired()
      .HasConversion<string>()
      .HasMaxLength(20);

    builder.Property(i => i.Code)
      .IsRequired()
      .HasMaxLength(20);

    builder.Property(i => i.CreatedAt)
      .IsRequired();

    builder.Property(i => i.ExpiresAt);

    builder.Property(i => i.IsActive)
      .IsRequired()
      .HasDefaultValue(true);

    builder.Property(i => i.CreatedBy)
      .IsRequired();

    // Create index on Code for fast lookup
    builder.HasIndex(i => i.Code)
      .IsUnique();

    // Create index on FamilyId for queries
    builder.HasIndex(i => i.FamilyId);

    // Create composite index for active invitations
    builder.HasIndex(i => new { i.IsActive, i.ExpiresAt });
  }
}
