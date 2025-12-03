using FamilyTaskManager.UnitTests.Host.Bot.Models;

namespace FamilyTaskManager.Host.Modules.Bot.Models;

public class UserSession
{
  public required long TelegramId { get; init; }
  public required Guid UserId { get; init; }
  public Guid? CurrentFamilyId { get; set; }

  public ConversationState State { get; set; } = ConversationState.None;
  public UserSessionData Data { get; set; } = new();
  public DateTime LastActivity { get; set; } = DateTime.UtcNow;

  public void UpdateActivity() => LastActivity = DateTime.UtcNow;

  public void ClearState()
  {
    State = ConversationState.None;
    Data = new();
    UpdateActivity();
  }

  public void SetState(ConversationState newState, UserSessionData data)
  {
    State = newState;
    Data = data;
    UpdateActivity();
  }
}
