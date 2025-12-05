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
  public const string Back = "back";
  public const string Select = "select";
  public const string Confirm = "confirm";
  public const string Cancel = "cancel";
  public const string Invite = "invite";

  // Spot-specific actions
  public const string ConfirmDelete = "confirmdelete";
  public const string CancelDelete = "canceldelete";

  // Task-specific actions
  public const string Complete = "complete";
  public const string Take = "take";

  // Template-specific actions (short codes)
  public const string ViewForSpot = "vp"; // view templates for spot
  public const string CreateForSpot = "cf"; // create template for spot
  public const string CreateTask = "ct"; // create task from template
  public const string EditField = "ef"; // edit template field

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
  public const string ShowList = "showlist";
  public const string Detect = "detect";
}
