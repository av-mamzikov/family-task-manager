namespace FamilyTaskManager.Host.Modules.Bot.Models;

public class UserSession
{
  public Guid? CurrentFamilyId { get; set; }

  public ConversationState State { get; set; } = ConversationState.None;
  public Dictionary<string, object> Data { get; set; } = new();
  public DateTime LastActivity { get; set; } = DateTime.UtcNow;

  public bool IsDirty { get; set; }

  public void UpdateActivity()
  {
    LastActivity = DateTime.UtcNow;
    IsDirty = true;
  }

  public void SetState(ConversationState state, Dictionary<string, object>? data = null)
  {
    State = state;
    if (data != null)
    {
      Data = data;
    }

    UpdateActivity();
    IsDirty = true;
  }

  public void ClearState()
  {
    State = ConversationState.None;
    Data.Clear();
    UpdateActivity();
    IsDirty = true;
  }

  public void MarkClean() => IsDirty = false;
}

public enum ConversationState
{
  None,
  AwaitingFamilyName,
  AwaitingFamilyTimezone,
  AwaitingFamilyLocation,
  AwaitingPetType,
  AwaitingPetName,
  AwaitingTaskType,
  AwaitingTaskTitle,
  AwaitingTaskPoints,
  AwaitingTaskPetSelection,
  AwaitingTaskSchedule,
  AwaitingTaskDueDate,
  AwaitingTemplateTitle,
  AwaitingTemplatePoints,
  AwaitingTemplateScheduleType,
  AwaitingTemplateScheduleTime,
  AwaitingTemplateScheduleWeekday,
  AwaitingTemplateScheduleMonthDay,
  AwaitingTemplateDueDuration,
  AwaitingTemplatePetSelection,
  AwaitingTemplateEditTitle,
  AwaitingTemplateEditPoints,
  AwaitingTemplateEditScheduleType,
  AwaitingTemplateEditScheduleTime,
  AwaitingTemplateEditScheduleWeekday,
  AwaitingTemplateEditScheduleMonthDay,
  AwaitingTemplateEditDueDuration,
  AwaitingTaskScheduleType,
  AwaitingTaskScheduleTime,
  AwaitingTaskScheduleWeekday,
  AwaitingTaskScheduleMonthDay,
  AwaitingTemplateSchedule,
  AwaitingTemplateEditSchedule
}
