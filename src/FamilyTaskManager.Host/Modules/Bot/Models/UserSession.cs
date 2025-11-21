namespace FamilyTaskManager.Host.Modules.Bot.Models;

public class UserSession
{
  public Guid? CurrentFamilyId { get; set; }
  public ConversationState State { get; set; } = ConversationState.None;
  public Dictionary<string, object> Data { get; set; } = new();
  public DateTime LastActivity { get; set; } = DateTime.UtcNow;

  public void UpdateActivity()
  {
    LastActivity = DateTime.UtcNow;
  }

  public void SetState(ConversationState state, Dictionary<string, object>? data = null)
  {
    State = state;
    if (data != null)
    {
      Data = data;
    }
    UpdateActivity();
  }

  public void ClearState()
  {
    State = ConversationState.None;
    Data.Clear();
    UpdateActivity();
  }
}

public enum ConversationState
{
  None,
  AwaitingFamilyName,
  AwaitingPetType,
  AwaitingPetName,
  AwaitingTaskType,
  AwaitingTaskTitle,
  AwaitingTaskPoints,
  AwaitingTaskPetSelection,
  AwaitingTaskSchedule,
  AwaitingTaskDueDate
}
