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
        InlineKeyboardButton.WithCallbackData("⭐", "points:1"),
        InlineKeyboardButton.WithCallbackData("⭐⭐", "points:2"),
        InlineKeyboardButton.WithCallbackData("⭐⭐⭐", "points:3")
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
