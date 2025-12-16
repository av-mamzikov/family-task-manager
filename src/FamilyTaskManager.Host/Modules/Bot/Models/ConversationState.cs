namespace FamilyTaskManager.Host.Modules.Bot.Models;

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
  Stats
}
