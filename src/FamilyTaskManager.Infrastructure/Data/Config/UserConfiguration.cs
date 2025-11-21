using FamilyTaskManager.Core.UserAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyTaskManager.Infrastructure.Data.Config;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
  public void Configure(EntityTypeBuilder<User> builder)
  {
    builder.ToTable("Users");

    builder.HasKey(u => u.Id);

    builder.Property(u => u.TelegramId)
      .IsRequired();

    builder.HasIndex(u => u.TelegramId)
      .IsUnique();

    builder.Property(u => u.Name)
      .IsRequired()
      .HasMaxLength(200);

    builder.Property(u => u.CreatedAt)
      .IsRequired();
  }
}
