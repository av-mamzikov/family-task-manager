using Telegram.Bot.Types.ReplyMarkups;

namespace FamilyTaskManager.Host.Modules.Bot.Helpers;

public static class TaskPointsHelper
{
  /// <summary>
  ///   Creates inline keyboard with star buttons for selecting task points (1-3).
  /// </summary>
  public static InlineKeyboardMarkup GetPointsSelectionKeyboard() =>
    new(new[]
    {
      new[]
      {
        InlineKeyboardButton.WithCallbackData("⭐", "points_1"),
        InlineKeyboardButton.WithCallbackData("⭐⭐", "points_2"),
        InlineKeyboardButton.WithCallbackData("⭐⭐⭐", "points_3")
      },
      new[]
      {
        InlineKeyboardButton.WithCallbackData("⬅️ Назад", "points_back")
      }
    });

  /// <summary>
  ///   Converts points value to star representation.
  /// </summary>
  public static string ToStars(int points) => points switch
  {
    1 => "⭐",
    2 => "⭐⭐",
    3 => "⭐⭐⭐",
    _ => points.ToString()
  };
}
