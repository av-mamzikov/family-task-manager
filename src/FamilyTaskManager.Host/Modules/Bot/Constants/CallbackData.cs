using FamilyTaskManager.Host.Modules.Bot.Models;

namespace FamilyTaskManager.Host.Modules.Bot.Constants;

public static class CallbackData
{
  public static class SpotBrowsing
  {
    public const ConversationState Conversation = ConversationState.SpotBrowsing;

    public static string List() => $"{Conversation}_{CallbackActions.List}";
    public static string View(EncodedGuid spotId) => $"{Conversation}_{CallbackActions.View}_{spotId}";

    public static string Delete(EncodedGuid spotId) => $"{Conversation}_{CallbackActions.Delete}_{spotId}";

    public static string ConfirmDelete(EncodedGuid spotId) =>
      $"{Conversation}_{CallbackActions.ConfirmDelete}_{spotId}";

    public static string ResponsibleList(EncodedGuid spotId) =>
      $"{Conversation}_{CallbackActions.ResponsibleList}_{spotId}";

    public static string ResponsibleToggle(EncodedGuid spotId, EncodedGuid memberId) =>
      $"{Conversation}_{CallbackActions.ResponsibleToggle}_{spotId}_{memberId}";
  }

  public static class SpotCreation
  {
    public const ConversationState Conversation = ConversationState.SpotCreation;

    public static string Start() => $"{Conversation}_{CallbackActions.Start}";

    public static string SelectType(string type) => $"{Conversation}_{CallbackActions.Select}_{type}";
  }

  public static class TemplateForm
  {
    public const ConversationState Conversation = ConversationState.TemplateForm;

    public static string Create(EncodedGuid spotId) => $"{Conversation}_{CallbackActions.Create}_{spotId}";
    public static string Edit(EncodedGuid templateId) => $"{Conversation}_{CallbackActions.Edit}_{templateId}";

    public static string EditField(EncodedGuid templateId, string fieldName) =>
      $"{Conversation}_{CallbackActions.Edit}_{templateId}_{fieldName}";

    public static string SelectPoints(int points) => $"{Conversation}_points_{points}";
    public static string SelectScheduleType(string scheduleType) => $"{Conversation}_schedule_{scheduleType}";
    public static string SelectWeekday(string weekday) => $"{Conversation}_weekday_{weekday}";
  }

  public static class TemplateBrowsing
  {
    public const ConversationState Conversation = ConversationState.TemplateBrowsing;

    public static string Create(EncodedGuid spotId) =>
      $"{Conversation}_{CallbackActions.Create}_{spotId}";

    public static string ListOfSpot(EncodedGuid spotId) =>
      $"{Conversation}_{CallbackActions.ListForSpot}_{spotId}";

    public static string View(EncodedGuid templateId) =>
      $"{Conversation}_{CallbackActions.View}_{templateId}";

    public static string CreateTask(EncodedGuid templateId) =>
      $"{Conversation}_{CallbackActions.CreateTask}_{templateId}";

    public static string Delete(EncodedGuid templateId) => $"{Conversation}_{CallbackActions.Delete}_{templateId}";

    public static string ConfirmDelete(EncodedGuid templateId) =>
      $"{Conversation}_{CallbackActions.ConfirmDelete}_{templateId}";
  }

  public static class Family
  {
    public const ConversationState Conversation = ConversationState.Family;

    public static string Create() => $"{Conversation}_{CallbackActions.Create}";

    public static string Select(EncodedGuid familyId) => $"{Conversation}_{CallbackActions.Select}_{familyId}";
    public static string Invite() => $"{Conversation}_{CallbackActions.Invite}";
    public static string Settings() => $"{Conversation}_{CallbackActions.Settings}";
    public static string Delete() => $"{Conversation}_{CallbackActions.Delete}";
    public static string List() => $"{Conversation}_{CallbackActions.List}";

    public static string ConfirmDelete(EncodedGuid familyId) =>
      $"{Conversation}_{CallbackActions.ConfirmDelete}_{familyId}";

    public static string InviteRole(EncodedGuid familyId, string role) =>
      $"{Conversation}_{CallbackActions.Invite}_role_{familyId}_{role}";
  }

  public static class FamilyCreation
  {
    public const ConversationState Conversation = ConversationState.FamilyCreation;

    public static string DetectTimezone() => $"{Conversation}_{CallbackActions.DetectTimezone}";
    public static string ShowTimezoneList() => $"{Conversation}_{CallbackActions.ShowTimezoneList}";

    public static string TimeZone(string timeZoneCode) => $"{Conversation}_{CallbackActions.Timezone}_{timeZoneCode}";
  }

  public static class FamilyMembers
  {
    public const ConversationState Conversation = ConversationState.FamilyMembers;

    public static string List() => $"{Conversation}_{CallbackActions.List}";
    public static string Member(EncodedGuid memberCode) => $"{Conversation}_{CallbackActions.Member}_{memberCode}";

    public static string ChangeRole(EncodedGuid memberCode) =>
      $"{Conversation}_{CallbackActions.MemberRole}_{memberCode}";

    public static string Delete(EncodedGuid memberCode) =>
      $"{Conversation}_{CallbackActions.MemberDelete}_{memberCode}";

    public static string ConfirmDelete(EncodedGuid memberCode) =>
      $"{Conversation}_{CallbackActions.MemberDeleteOk}_{memberCode}";

    public static string PickRole(EncodedGuid memberCode, int role) =>
      $"{Conversation}_{CallbackActions.MemberRolePick}_{memberCode}_{role}";
  }

  public static class TaskBrowsing
  {
    public const ConversationState Conversation = ConversationState.TaskBrowsing;

    public static string List() => $"{Conversation}_{CallbackActions.List}";

    public static string Complete(EncodedGuid taskId) => $"{Conversation}_{CallbackActions.Complete}_{taskId}";
    public static string Refuse(EncodedGuid taskId) => $"{Conversation}_{CallbackActions.Cancel}_{taskId}";
    public static string Take(EncodedGuid taskId) => $"{Conversation}_{CallbackActions.Take}_{taskId}";
    public static string Delete(EncodedGuid taskId) => $"{Conversation}_{CallbackActions.Delete}_{taskId}";
  }

  public static class Stats
  {
    public const ConversationState Conversation = ConversationState.StatsBrowsing;

    public static string List() => $"{Conversation}_{CallbackActions.List}";
  }
}
