using FamilyTaskManager.Infrastructure.Notifications;

namespace FamilyTaskManager.Infrastructure.Data.Config;

public class DomainEventOutboxConfiguration : IEntityTypeConfiguration<DomainEventOutbox>
{
  public void Configure(EntityTypeBuilder<DomainEventOutbox> builder)
  {
    builder.ToTable("DomainEventOutbox");

    builder.HasKey(o => o.Id);

    builder.Property(o => o.EventType)
      .IsRequired()
      .HasMaxLength(100);

    builder.Property(o => o.Payload)
      .IsRequired();

    builder.Property(o => o.OccurredAtUtc)
      .IsRequired();

    builder.Property(o => o.ProcessedAtUtc)
      .IsRequired(false);

    builder.Property(o => o.Attempts)
      .IsRequired()
      .HasDefaultValue(0);

    builder.Property(o => o.Status)
      .IsRequired()
      .HasConversion<string>()
      .HasMaxLength(20);

    // Indexes for efficient querying
    builder.HasIndex(o => new { o.Status, o.OccurredAtUtc });
    builder.HasIndex(o => new { o.EventType, o.Status });
    builder.HasIndex(o => o.ProcessedAtUtc);
  }
}
