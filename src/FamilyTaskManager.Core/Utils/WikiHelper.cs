using FamilyTaskManager.Core.UserAggregate;

namespace FamilyTaskManager.Core.Utils;

public static class WikiHelper
{
  public static string GetUserLink(string name, long telegramId) => $"[{name}](tg://user?id={telegramId})";

  public static string GetUserLink(User user) => GetUserLink(user.Name, user.TelegramId);
}
