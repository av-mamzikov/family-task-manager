namespace FamilyTaskManager.Core.UserAggregate;

public class User : EntityBase<User, Guid>, IAggregateRoot
{
  public long TelegramId { get; private set; }
  public string Name { get; private set; } = null!;
  public DateTime CreatedAt { get; private set; }

  private User() { }

  public User(long telegramId, string name)
  {
    Guard.Against.NegativeOrZero(telegramId);
    Guard.Against.NullOrWhiteSpace(name);

    TelegramId = telegramId;
    Name = name.Trim();
    CreatedAt = DateTime.UtcNow;
  }

  public void UpdateName(string name)
  {
    Guard.Against.NullOrWhiteSpace(name);

    Name = name.Trim();
  }
}
