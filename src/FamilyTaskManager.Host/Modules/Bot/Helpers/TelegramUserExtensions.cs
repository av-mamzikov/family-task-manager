using Telegram.Bot.Types;

namespace FamilyTaskManager.Host.Modules.Bot.Helpers;

/// <summary>
///   Extension methods for Telegram User type
/// </summary>
public static class TelegramUserExtensions
{
  /// <summary>
  ///   Gets the best display name for a Telegram user
  ///   Priority: FirstName + LastName, then FirstName, then LastName, then @Username, then "User"
  /// </summary>
  /// <param name="user">The Telegram user</param>
  /// <returns>The display name</returns>
  public static string GetDisplayName(this User user)
  {
    if (user == null)
    {
      return "User";
    }

    // Try FirstName + LastName first (only if both exist)
    if (!string.IsNullOrEmpty(user.FirstName) && !string.IsNullOrEmpty(user.LastName))
    {
      return $"{user.FirstName} {user.LastName}";
    }

    // Try FirstName alone
    if (!string.IsNullOrEmpty(user.FirstName))
    {
      return user.FirstName;
    }

    // Try LastName alone
    if (!string.IsNullOrEmpty(user.LastName))
    {
      return user.LastName;
    }

    // Try @Username
    if (!string.IsNullOrEmpty(user.Username))
    {
      return $"@{user.Username}";
    }

    // Fallback to "User"
    return "User";
  }
}
