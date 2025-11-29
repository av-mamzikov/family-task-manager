using FamilyTaskManager.Core.TaskAggregate;

namespace FamilyTaskManager.Infrastructure.Data.Config;

public class TaskTemplateConfiguration : IEntityTypeConfiguration<TaskTemplate>
{
  public void Configure(EntityTypeBuilder<TaskTemplate> builder)
  {
    builder.ToTable("TaskTemplates");

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

    // Configure Schedule as owned entity (Value Object)
    builder.OwnsOne(t => t.Schedule, schedule =>
    {
      schedule.Property(s => s.Type)
        .HasConversion(
          v => v.Value,
          v => ScheduleType.FromValue(v))
        .HasColumnName("ScheduleType")
        .IsRequired();

      schedule.Property(s => s.Time)
        .HasColumnName("ScheduleTime")
        .IsRequired();

      schedule.Property(s => s.DayOfWeek)
        .HasConversion<int?>()
        .HasColumnName("ScheduleDayOfWeek");

      schedule.Property(s => s.DayOfMonth)
        .HasColumnName("ScheduleDayOfMonth");
    });

    builder.Property(t => t.CreatedBy)
      .IsRequired();

    builder.Property(t => t.CreatedAt)
      .IsRequired();

    builder.Property(t => t.IsActive)
      .IsRequired()
      .HasDefaultValue(true);

    // Indexes for optimization
    builder.HasIndex(t => t.FamilyId);
    builder.HasIndex(t => t.PetId);
    builder.HasIndex(t => new { t.FamilyId, t.IsActive });

    // Foreign key relationship to Family
    builder.HasOne(t => t.Family)
      .WithMany()
      .HasForeignKey(t => t.FamilyId)
      .OnDelete(DeleteBehavior.Cascade);

    // Foreign key relationship to Pet
    builder.HasOne(t => t.Pet)
      .WithMany()
      .HasForeignKey(t => t.PetId)
      .OnDelete(DeleteBehavior.Cascade);
  }
}
