using FamilyTaskManager.Host.Modules.Bot.Helpers;
using FamilyTaskManager.Host.Modules.Bot.Models;

namespace FamilyTaskManager.Host.Modules.Bot.Constants;

public static class CallbackData
{
  public static class SpotBowsing
  {
    public const ConversationState Conversation = ConversationState.SpotBrowsing;

    public static string Create() => $"{Conversation}_{CallbackActions.Create}";
    public static string CancelDelete() => $"{Conversation}_{CallbackActions.List}";

    public static string List() => $"{Conversation}_{CallbackActions.List}";
    public static string View(Guid spotId) => $"{Conversation}_{CallbackActions.View}_{spotId.EncodeToCallbackData()}";

    public static string Delete(Guid spotId) => $"{Conversation}_{CallbackActions.Delete}_{spotId}";

    public static string ConfirmDelete(Guid spotId) => $"{Conversation}_{CallbackActions.ConfirmDelete}_{spotId}";
  }

  public static class TemplateForm
  {
    public const ConversationState Conversation = ConversationState.TemplateForm;

    public static string Edit(Guid templateId) => $"{Conversation}_{CallbackActions.Edit}_{templateId}";
  }

  public static class TemplateBrowsing
  {
    public const ConversationState Conversation = ConversationState.TemplateBrowsing;

    public static string Create(Guid spotId) => $"{Conversation}_{CallbackActions.Create}_{spotId}";

    public static string ListOfSpot(Guid spotId) => $"{Conversation}_{CallbackActions.ListForSpot}_{spotId}";
    public static string View(Guid templateId) => $"{Conversation}_{CallbackActions.View}_{templateId}";
    public static string CreateTask(Guid templateId) => $"{Conversation}_{CallbackActions.CreateTask}_{templateId}";

    public static string EditField(Guid templateId, string fieldName) =>
      $"{Conversation}_{CallbackActions.Edit}_{templateId}_{fieldName}";

    public static string Delete(Guid templateId) => $"{Conversation}_{CallbackActions.Delete}_{templateId}";

    public static string ConfirmDelete(Guid templateId) =>
      $"{Conversation}_{CallbackActions.ConfirmDelete}_{templateId}";
  }

  public static class Family
  {
    public const ConversationState Conversation = ConversationState.Family;

    public static string Create => $"{Conversation}_{CallbackActions.Create}";

    public static string Select(Guid familyId) => $"{Conversation}_{CallbackActions.Select}_{familyId}";
    public static string Invite() => $"{Conversation}_{CallbackActions.Invite}";
    public static string Settings() => $"{Conversation}_{CallbackActions.Settings}";
    public static string Delete() => $"{Conversation}_{CallbackActions.Delete}";
    public static string List() => $"{Conversation}_{CallbackActions.List}";
    public static string ConfirmDelete(Guid familyId) => $"{Conversation}_{CallbackActions.ConfirmDelete}_{familyId}";

    public static string InviteRole(Guid familyId, string role) =>
      $"{Conversation}_{CallbackActions.Invite}_role_{familyId}_{role}";
  }

  public static class FamilyCreation
  {
    public const ConversationState Conversation = ConversationState.FamilyCreation;

    public static string DetectTimezone => $"{Conversation}_{CallbackActions.DetectTimezone}";
    public static string ShowTimezoneList => $"{Conversation}_{CallbackActions.ShowTimezoneList}";

    public static string EuropeKaliningrad => $"{Conversation}_{CallbackActions.Timezone}_Europe/Kaliningrad";
    public static string EuropeMoscow => $"{Conversation}_{CallbackActions.Timezone}_Europe/Moscow";
    public static string EuropeSamara => $"{Conversation}_{CallbackActions.Timezone}_Europe/Samara";
    public static string AsiaYekaterinburg => $"{Conversation}_{CallbackActions.Timezone}_Asia/Yekaterinburg";
    public static string AsiaOmsk => $"{Conversation}_{CallbackActions.Timezone}_Asia/Omsk";
    public static string AsiaKrasnoyarsk => $"{Conversation}_{CallbackActions.Timezone}_Asia/Krasnoyarsk";
    public static string AsiaIrkutsk => $"{Conversation}_{CallbackActions.Timezone}_Asia/Irkutsk";
    public static string AsiaYakutsk => $"{Conversation}_{CallbackActions.Timezone}_Asia/Yakutsk";
    public static string AsiaVladivostok => $"{Conversation}_{CallbackActions.Timezone}_Asia/Vladivostok";
    public static string AsiaMagadan => $"{Conversation}_{CallbackActions.Timezone}_Asia/Magadan";
    public static string AsiaKamchatka => $"{Conversation}_{CallbackActions.Timezone}_Asia/Kamchatka";
    public static string Utc => $"{Conversation}_{CallbackActions.Timezone}_UTC";
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
  }

  public static class Task
  {
    public const ConversationState Conversation = ConversationState.TaskBrowsing;

    public static string List() => $"{Conversation}_{CallbackActions.List}";
    public static string Create() => $"{Conversation}_{CallbackActions.Create}";

    public static string Complete(Guid taskId) => $"{Conversation}_{CallbackActions.Complete}_{taskId}";
    public static string Cancel(Guid taskId) => $"{Conversation}_{CallbackActions.Cancel}_{taskId}";
    public static string Take(Guid taskId) => $"{Conversation}_{CallbackActions.Take}_{taskId}";
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
      $"{SpotBowsing.Conversation}_{CallbackActions.Select}_{callbackCode}"; // spot_select_{code}
  }

  public static class Stats
  {
    public const ConversationState Conversation = ConversationState.StatsBrowsing;

    public static string List() => $"{Conversation}_{CallbackActions.List}";
  }
}
