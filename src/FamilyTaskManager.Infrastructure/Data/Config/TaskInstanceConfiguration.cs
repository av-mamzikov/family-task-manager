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

    builder.Property(t => t.PetId)
      .IsRequired();

    builder.Property(t => t.Title)
      .IsRequired()
      .HasMaxLength(100);

    builder.Property(t => t.Points)
      .IsRequired();

    builder.Property(t => t.Type)
      .IsRequired()
      .HasConversion<string>()
      .HasMaxLength(20);

    builder.Property(t => t.TemplateId)
      .IsRequired(false);

    builder.Property(t => t.Status)
      .IsRequired()
      .HasConversion<string>()
      .HasMaxLength(20);

    builder.Property(t => t.CompletedBy)
      .IsRequired(false);

    builder.Property(t => t.CompletedAt)
      .IsRequired(false);

    builder.Property(t => t.CreatedAt)
      .IsRequired();

    builder.Property(t => t.DueAt)
      .IsRequired();

    // Indexes for optimization
    builder.HasIndex(t => t.FamilyId);
    builder.HasIndex(t => t.PetId);
    builder.HasIndex(t => t.TemplateId);
    builder.HasIndex(t => new { t.FamilyId, t.Status });
    builder.HasIndex(t => new { t.Status, t.DueAt });

    // Foreign key relationship to Family
    builder.HasOne(t => t.Family)
      .WithMany()
      .HasForeignKey(t => t.FamilyId)
      .OnDelete(DeleteBehavior.Cascade);

    // Foreign key relationship to Pet
    builder.HasOne(t => t.Pet)
      .WithMany()
      .HasForeignKey(t => t.PetId)
      .OnDelete(DeleteBehavior.ClientCascade);

    // Foreign key relationship to TaskTemplate
    builder.HasOne(t => t.Template)
      .WithMany()
      .HasForeignKey(t => t.TemplateId)
      .OnDelete(DeleteBehavior.NoAction);

    // Foreign key relationship to User (CompletedBy)
    builder.HasOne(t => t.CompletedByUser)
      .WithMany()
      .HasForeignKey(t => t.CompletedBy)
      .OnDelete(DeleteBehavior.NoAction);
  }
}
