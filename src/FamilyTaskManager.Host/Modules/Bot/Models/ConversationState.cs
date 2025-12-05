namespace FamilyTaskManager.Host.Modules.Bot.Models;

public enum ConversationState
{
  None,
  FamilyCreation,
  SpotCreation,
  TaskCreation,
  TemplateForm,

  // Browsing conversations
  Family,
  FamilyMembers,
  SpotBrowsing,
  TaskBrowsing,
  TemplateBrowsing,
  StatsBrowsing
}
