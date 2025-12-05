namespace FamilyTaskManager.Host.Modules.Bot.Constants;

/// <summary>
///   Centralized constants for callback data actions.
/// </summary>
public static class CallbackActions
{
  // Common actions
  public const string Create = "create";
  public const string View = "view";
  public const string Delete = "delete";
  public const string Edit = "edit";
  public const string Select = "select";
  public const string Confirm = "confirm";
  public const string Cancel = "cancel";
  public const string Invite = "invite";

  // Spot-specific actions
  public const string ConfirmDelete = "confirmdelete";
  public const string List = "list";

  // Task-specific actions
  public const string Complete = "complete";
  public const string Take = "take";

  // Family member actions
  public const string Member = "member";
  public const string MemberRole = "memberrole";
  public const string MemberRolePick = "mrpick";
  public const string MemberDelete = "mdel";
  public const string MemberDeleteOk = "mdelok";

  // Settings and configuration
  public const string Settings = "settings";
  public const string Members = "members";

  // Schedule actions
  public const string Type = "type";
  public const string Weekday = "weekday";

  // Timezone actions
  public const string Timezone = "tz";
  public const string ShowTimezoneList = "showtzlist";
  public const string DetectTimezone = "detecttz";

  // template browsing actions
  public const string CreateTask = "createTask";
  public const string ListForSpot = "listForSpot";
}
