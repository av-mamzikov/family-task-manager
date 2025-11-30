using FamilyTaskManager.Core.UserAggregate;

namespace FamilyTaskManager.Infrastructure.Data.Config;

public class TelegramSessionConfiguration : IEntityTypeConfiguration<TelegramSession>
{
  public void Configure(EntityTypeBuilder<TelegramSession> builder)
  {
    builder.ToTable("TelegramSessions");

    builder.HasKey(s => s.Id);

    builder.Property(s => s.UserId)
      .IsRequired();

    builder.HasIndex(s => s.UserId)
      .IsUnique();

    builder.Property(s => s.CurrentFamilyId)
      .IsRequired(false);

    builder.Property(s => s.ConversationState)
      .IsRequired()
      .HasDefaultValue(0);

    builder.Property(s => s.SessionData)
      .IsRequired(false);

    builder.Property(s => s.LastActivity)
      .IsRequired();

    // 1:1 relationship with User
    builder.HasOne(s => s.User)
      .WithOne()
      .HasForeignKey<TelegramSession>(s => s.UserId)
      .OnDelete(DeleteBehavior.Cascade);
  }
}
