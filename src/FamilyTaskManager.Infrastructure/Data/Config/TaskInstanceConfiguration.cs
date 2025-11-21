using FamilyTaskManager.Core.TaskAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

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
  }
}
