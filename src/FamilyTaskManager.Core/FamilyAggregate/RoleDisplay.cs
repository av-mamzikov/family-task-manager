namespace FamilyTaskManager.Core.FamilyAggregate;

public class RoleDisplay
{
  public static (string emoji, string text) GetRoleInfo(FamilyRole role) => role switch
  {
    FamilyRole.Admin => ("👑", "Администратор"),
    FamilyRole.Adult => ("👤", "Взрослый"),
    FamilyRole.Child => ("👶", "Ребёнок"),
    _ => ("❓", "Неизвестно")
  };

  public static string GetRoleCaption(FamilyRole entryRole)
  {
    var info = GetRoleInfo(entryRole);
    return $"{info.emoji} {info.text}";
  }

  public static string GetRoleEmoji(FamilyRole entryRole)
  {
    var info = GetRoleInfo(entryRole);
    return info.emoji;
  }
}
