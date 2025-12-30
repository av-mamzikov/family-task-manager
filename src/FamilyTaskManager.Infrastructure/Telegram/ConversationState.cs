namespace FamilyTaskManager.Infrastructure.Telegram;

public enum ConversationState
{
  None,
  FamilyCreation,
  SpotCreation,
  TemplateForm,

  // Browsing conversations
  Family,
  Families,
  Spots,
  Tasks,
  Templates,
  Stats,
  Store
}
