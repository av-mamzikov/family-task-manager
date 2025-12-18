using FamilyTaskManager.Core.TaskAggregate;

namespace FamilyTaskManager.Infrastructure.Data.Config;

public class TaskInstanceConfiguration : IEntityTypeConfiguration<TaskInstance>
{
  public void Configure(EntityTypeBuilder<TaskInstance> builder)
  {
    builder.ToTable("TaskInstances");

    builder.HasKey(t => t.Id);

    builder.Property(t => t.FamilyId)
      .IsRequired();

    builder.Property(t => t.SpotId)
      .IsRequired();

    builder.Property(t => t.Title)
      .IsRequired()
      .HasMaxLength(100);

    builder.Property(t => t.Points)
      .IsRequired()
      .HasConversion(
        v => v.Value,
        v => new(v));

    builder.Property(t => t.TemplateId)
      .IsRequired(false);

    builder.Property(t => t.Status)
      .IsRequired()
      .HasConversion<string>()
      .HasMaxLength(20);

    builder.Property(t => t.AssignedToMemberId)
      .IsRequired(false);

    builder.Property(t => t.CompletedAt)
      .IsRequired(false);

    builder.Property(t => t.CreatedAt)
      .IsRequired();

    builder.Property(t => t.DueAt)
      .IsRequired();

    // Indexes for optimization
    builder.HasIndex(t => t.FamilyId);
    builder.HasIndex(t => t.SpotId);
    builder.HasIndex(t => t.TemplateId);
    builder.HasIndex(t => t.AssignedToMemberId);
    builder.HasIndex(t => new { t.FamilyId, t.Status });
    builder.HasIndex(t => new { t.Status, t.DueAt });

    // Foreign key relationship to Family
    builder.HasOne(t => t.Family)
      .WithMany()
      .HasForeignKey(t => t.FamilyId)
      .OnDelete(DeleteBehavior.Cascade);

    // Foreign key relationship to Spot
    builder.HasOne(t => t.Spot)
      .WithMany()
      .HasForeignKey(t => t.SpotId)
      .OnDelete(DeleteBehavior.Cascade);

    builder
      .Navigation(p => p.Spot)
      .UsePropertyAccessMode(PropertyAccessMode.Field)
      .AutoInclude();

    // Foreign key relationship to TaskTemplate
    builder.HasOne(t => t.Template)
      .WithMany()
      .HasForeignKey(t => t.TemplateId)
      .OnDelete(DeleteBehavior.SetNull);

    // Foreign key relationship to FamilyMember (AssignedTo)
    builder.HasOne(t => t.AssignedToMember)
      .WithMany()
      .HasForeignKey(t => t.AssignedToMemberId)
      .OnDelete(DeleteBehavior.SetNull);

    builder.Navigation(e => e.AssignedToMember).AutoInclude();
  }
}
