namespace FamilyTaskManager.Host.Modules.Bot.Constants;

public static class CallbackData
{
  public static class Spot
  {
    public const string Entity = "spot";

    public const string Create = $"{Entity}_{CallbackActions.Create}";
    public const string Back = $"{Entity}_{CallbackActions.Back}";
    public const string CancelDelete = $"{Entity}_{CallbackActions.CancelDelete}";

    public static string View(Guid spotId) => $"{Entity}_{CallbackActions.View}_{spotId}";
    public static string Delete(Guid spotId) => $"{Entity}_{CallbackActions.Delete}_{spotId}";
    public static string ConfirmDelete(Guid spotId) => $"{Entity}_{CallbackActions.ConfirmDelete}_{spotId}";
  }

  public static class Templates
  {
    public const string Entity = "tpl";

    public const string CreateRoot = $"{Entity}_{CallbackActions.Create}";
    public const string Back = $"{Entity}_{CallbackActions.Back}";

    public static string ViewForSpot(Guid spotId) => $"{Entity}_{CallbackActions.ViewForSpot}_{spotId}";
    public static string CreateForSpot(Guid spotId) => $"{Entity}_{CallbackActions.CreateForSpot}_{spotId}";
    public static string View(Guid templateId) => $"{Entity}_{CallbackActions.View}_{templateId}";
    public static string CreateTask(Guid templateId) => $"{Entity}_{CallbackActions.CreateTask}_{templateId}";
    public static string Edit(Guid templateId) => $"{Entity}_{CallbackActions.Edit}_{templateId}";
    public static string Delete(Guid templateId) => $"{Entity}_{CallbackActions.Delete}_{templateId}";
    public static string ConfirmDelete(Guid templateId) => $"{Entity}_{CallbackActions.ConfirmDelete}_{templateId}";

    public static string EditField(Guid templateId, char fieldCode) =>
      $"{Entity}_{CallbackActions.EditField}_{templateId}_{fieldCode}";

    public static string BackToSpot(Guid spotId) => $"{Entity}_{CallbackActions.ViewForSpot}_{spotId}";
    public static string BackToTemplate(Guid templateId) => $"{Entity}_{CallbackActions.View}_{templateId}";
  }

  public static class Family
  {
    public const string Entity = "family";

    public const string Create = $"{Entity}_{CallbackActions.Create}";

    public static string Select(Guid familyId) => $"{Entity}_{CallbackActions.Select}_{familyId}";
    public static string Members(Guid familyId) => $"{Entity}_{CallbackActions.Members}_{familyId}";
    public static string Invite(Guid familyId) => $"{Entity}_{CallbackActions.Invite}_{familyId}";
    public static string Settings(Guid familyId) => $"{Entity}_{CallbackActions.Settings}_{familyId}";
    public static string Delete(Guid familyId) => $"{Entity}_{CallbackActions.Delete}_{familyId}";
    public static string CancelDelete(Guid familyId) => $"{Entity}_{CallbackActions.CancelDelete}_{familyId}";
    public static string ConfirmDelete(Guid familyId) => $"{Entity}_{CallbackActions.ConfirmDelete}_{familyId}";

    public static string InviteRole(Guid familyId, string role) =>
      $"{Entity}_{CallbackActions.Invite}_role_{familyId}_{role}";
  }

  public static class FamilyMembers
  {
    public const string Entity = "family";

    public static string Member(string memberCode) => $"{Entity}_{CallbackActions.Member}_{memberCode}";
    public static string ChangeRole(string memberCode) => $"{Entity}_{CallbackActions.MemberRole}_{memberCode}";
    public static string Delete(string memberCode) => $"{Entity}_{CallbackActions.MemberDelete}_{memberCode}";
    public static string ConfirmDelete(string memberCode) => $"{Entity}_{CallbackActions.MemberDeleteOk}_{memberCode}";

    public static string PickRole(string memberCode, int role) =>
      $"{Entity}_{CallbackActions.MemberRolePick}_{memberCode}_{role}";

    public static string Invite(string familyCode) => $"{Entity}_{CallbackActions.Invite}_{familyCode}";
    public static string Back(string familyCode) => $"{Entity}_{CallbackActions.Back}_{familyCode}";
  }

  public static class Task
  {
    public const string Entity = "task";

    public const string Create = $"{Entity}_{CallbackActions.Create}";

    public static string Complete(Guid taskId) => $"{Entity}_{CallbackActions.Complete}_{taskId}";
    public static string Cancel(Guid taskId) => $"{Entity}_{CallbackActions.Cancel}_{taskId}";
    public static string Take(Guid taskId) => $"{Entity}_{CallbackActions.Take}_{taskId}";
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
    public const string TypeBack = $"{Entity}_{CallbackActions.Type}_{CallbackActions.Back}";

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
    public const string Back = $"{Entity}_{CallbackActions.Back}";
  }

  public static class SpotType
  {
    public static string Select(string callbackCode) =>
      $"{Spot.Entity}_{CallbackActions.Select}_{callbackCode}"; // spot_select_{code}
  }
}
