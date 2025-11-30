namespace FamilyTaskManager.Core.UserAggregate;

public class TelegramSession : EntityBase<TelegramSession, Guid>, IAggregateRoot
{
  private TelegramSession()
  {
  }

  public TelegramSession(Guid userId)
  {
    Id = Guid.NewGuid();
    UserId = userId;
    ConversationState = 0; // None
    SessionData = null;
    CurrentFamilyId = null;
    LastActivity = DateTime.UtcNow;
  }

  public Guid UserId { get; private set; }
  public Guid? CurrentFamilyId { get; private set; }
  public int ConversationState { get; private set; }
  public string? SessionData { get; private set; }
  public DateTime LastActivity { get; private set; }

  // Navigation property
  public User User { get; private set; } = null!;

  public void UpdateSession(Guid? currentFamilyId, int conversationState, string? sessionData)
  {
    CurrentFamilyId = currentFamilyId;
    ConversationState = conversationState;
    SessionData = sessionData;
    LastActivity = DateTime.UtcNow;
  }

  public void ClearSession()
  {
    CurrentFamilyId = null;
    ConversationState = 0; // None
    SessionData = null;
    LastActivity = DateTime.UtcNow;
  }

  public void UpdateActivity() => LastActivity = DateTime.UtcNow;
}
