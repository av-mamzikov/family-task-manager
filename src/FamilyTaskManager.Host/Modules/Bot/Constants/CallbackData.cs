using FamilyTaskManager.Host.Modules.Bot.Models;

namespace FamilyTaskManager.Host.Modules.Bot.Constants;

public static class CallbackData
{
  public static class Spot
  {
    public const ConversationState Conversation = ConversationState.SpotBrowsing;

    public static string Create => $"{Conversation}_{CallbackActions.Create}";
    public static string CancelDelete => $"{Conversation}_{CallbackActions.List}";

    public static string List() => $"{Conversation}_{CallbackActions.List}";
    public static string View(Guid spotId) => $"{Conversation}_{CallbackActions.View}_{spotId}";
    public static string Delete(Guid spotId) => $"{Conversation}_{CallbackActions.Delete}_{spotId}";
    public static string ConfirmDelete(Guid spotId) => $"{Conversation}_{CallbackActions.ConfirmDelete}_{spotId}";
  }

  public static class Templates
  {
    public const ConversationState Conversation = ConversationState.TemplateBrowsing;

    public static string Create => $"{Conversation}_{CallbackActions.Create}";

    public static string ViewForSpot(Guid spotId) => $"{Conversation}_{CallbackActions.ViewForSpot}_{spotId}";
    public static string CreateForSpot(Guid spotId) => $"{Conversation}_{CallbackActions.CreateForSpot}_{spotId}";
    public static string View(Guid templateId) => $"{Conversation}_{CallbackActions.View}_{templateId}";
    public static string CreateTask(Guid templateId) => $"{Conversation}_{CallbackActions.CreateTask}_{templateId}";
    public static string Edit(Guid templateId) => $"{Conversation}_{CallbackActions.Edit}_{templateId}";
    public static string Delete(Guid templateId) => $"{Conversation}_{CallbackActions.Delete}_{templateId}";

    public static string ConfirmDelete(Guid templateId) =>
      $"{Conversation}_{CallbackActions.ConfirmDelete}_{templateId}";

    public static string EditField(Guid templateId, char fieldCode) =>
      $"{Conversation}_{CallbackActions.EditField}_{templateId}_{fieldCode}";

    public static string BackToSpot(Guid spotId) => $"{Conversation}_{CallbackActions.ViewForSpot}_{spotId}";
    public static string BackToTemplate(Guid templateId) => $"{Conversation}_{CallbackActions.View}_{templateId}";
  }

  public static class Family
  {
    public const ConversationState Conversation = ConversationState.Family;

    public static string Create => $"{Conversation}_{CallbackActions.Create}";

    public static string Select() => $"{Conversation}_{CallbackActions.Select}";
    public static string Invite() => $"{Conversation}_{CallbackActions.Invite}";
    public static string Settings() => $"{Conversation}_{CallbackActions.Settings}";
    public static string Delete() => $"{Conversation}_{CallbackActions.Delete}";
    public static string List() => $"{Conversation}_{CallbackActions.List}";
    public static string ConfirmDelete(Guid familyId) => $"{Conversation}_{CallbackActions.ConfirmDelete}_{familyId}";

    public static string InviteRole(Guid familyId, string role) =>
      $"{Conversation}_{CallbackActions.Invite}_role_{familyId}_{role}";
  }

  public static class FamilyMembers
  {
    public const ConversationState Conversation = ConversationState.FamilyMembers;

    public static string Members() => $"{Conversation}_{CallbackActions.Members}";
    public static string Member(string memberCode) => $"{Conversation}_{CallbackActions.Member}_{memberCode}";
    public static string ChangeRole(string memberCode) => $"{Conversation}_{CallbackActions.MemberRole}_{memberCode}";
    public static string Delete(string memberCode) => $"{Conversation}_{CallbackActions.MemberDelete}_{memberCode}";

    public static string ConfirmDelete(string memberCode) =>
      $"{Conversation}_{CallbackActions.MemberDeleteOk}_{memberCode}";

    public static string PickRole(string memberCode, int role) =>
      $"{Conversation}_{CallbackActions.MemberRolePick}_{memberCode}_{role}";

    public static string Invite() => $"{Conversation}_{CallbackActions.Invite}";
  }

  public static class Task
  {
    public const ConversationState Conversation = ConversationState.TaskBrowsing;

    public static string Create => $"{Conversation}_{CallbackActions.Create}";

    public static string Complete(Guid taskId) => $"{Conversation}_{CallbackActions.Complete}_{taskId}";
    public static string Cancel(Guid taskId) => $"{Conversation}_{CallbackActions.Cancel}_{taskId}";
    public static string Take(Guid taskId) => $"{Conversation}_{CallbackActions.Take}_{taskId}";
  }

  public static class Timezone
  {
    public const string Entity = "timezone";

    public const string EuropeKaliningrad = Entity + "_Europe/Kaliningrad";
    public const string EuropeMoscow = Entity + "_Europe/Moscow";
    public const string EuropeSamara = Entity + "_Europe/Samara";
    public const string AsiaYekaterinburg = Entity + "_Asia/Yekaterinburg";
    public const string AsiaOmsk = Entity + "_Asia/Omsk";
    public const string AsiaKrasnoyarsk = Entity + "_Asia/Krasnoyarsk";
    public const string AsiaIrkutsk = Entity + "_Asia/Irkutsk";
    public const string AsiaYakutsk = Entity + "_Asia/Yakutsk";
    public const string AsiaVladivostok = Entity + "_Asia/Vladivostok";
    public const string AsiaMagadan = Entity + "_Asia/Magadan";
    public const string AsiaKamchatka = Entity + "_Asia/Kamchatka";
    public const string Utc = Entity + "_UTC";

    public static string Detect => $"{Entity}_{CallbackActions.Detect}";
    public static string ShowList => $"{Entity}_{CallbackActions.ShowList}";
  }

  public static class Schedule
  {
    public const string Entity = "schedule";

    public const string TypeDaily = $"{Entity}_{CallbackActions.Type}_daily";
    public const string TypeWorkdays = $"{Entity}_{CallbackActions.Type}_workdays";
    public const string TypeWeekends = $"{Entity}_{CallbackActions.Type}_weekends";
    public const string TypeWeekly = $"{Entity}_{CallbackActions.Type}_weekly";
    public const string TypeMonthly = $"{Entity}_{CallbackActions.Type}_monthly";
    public const string TypeManual = $"{Entity}_{CallbackActions.Type}_manual";

    public const string WeekdayMonday = $"{Entity}_{CallbackActions.Weekday}_monday";
    public const string WeekdayTuesday = $"{Entity}_{CallbackActions.Weekday}_tuesday";
    public const string WeekdayWednesday = $"{Entity}_{CallbackActions.Weekday}_wednesday";
    public const string WeekdayThursday = $"{Entity}_{CallbackActions.Weekday}_thursday";
    public const string WeekdayFriday = $"{Entity}_{CallbackActions.Weekday}_friday";
    public const string WeekdaySaturday = $"{Entity}_{CallbackActions.Weekday}_saturday";
    public const string WeekdaySunday = $"{Entity}_{CallbackActions.Weekday}_sunday";
  }

  public static class Points
  {
    public const string Entity = "points";

    public const string One = $"{Entity}_1";
    public const string Two = $"{Entity}_2";
    public const string Three = $"{Entity}_3";
    public const string Four = $"{Entity}_4";
  }

  public static class SpotType
  {
    public static string Select(string callbackCode) =>
      $"{Spot.Conversation}_{CallbackActions.Select}_{callbackCode}"; // spot_select_{code}
  }

  public static class Stats
  {
    public const ConversationState Conversation = ConversationState.StatsBrowsing;

    public static string Browse() => $"{Conversation}";
  }
}
