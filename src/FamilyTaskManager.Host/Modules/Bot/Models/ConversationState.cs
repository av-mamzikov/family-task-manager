namespace FamilyTaskManager.Host.Modules.Bot.Models;

public enum ConversationState
{
  None,
  FamilyCreation,
  SpotCreation,
  TemplateForm,

  // Browsing conversations
  Family,
  FamilyMembers,
  SpotBrowsing,
  TaskBrowsing,
  TemplateBrowsing,
  StatsBrowsing
}
