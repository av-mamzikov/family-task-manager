using FamilyTaskManager.Host.Modules.Bot.Constants;
using Telegram.Bot.Types.ReplyMarkups;

namespace FamilyTaskManager.Host.Modules.Bot.Helpers;

public static class TaskPointsHelper
{
  /// <summary>
  ///   Creates inline keyboard with star buttons for selecting task points (1-3).
  /// </summary>
  public static InlineKeyboardMarkup GetPointsSelectionKeyboard(string backCallBackData) =>
    new([
      [
        InlineKeyboardButton.WithCallbackData("⭐", CallbackData.Points.One),
        InlineKeyboardButton.WithCallbackData("⭐⭐", CallbackData.Points.Two),
        InlineKeyboardButton.WithCallbackData("⭐⭐⭐", CallbackData.Points.Three),
        InlineKeyboardButton.WithCallbackData("⭐⭐⭐⭐", CallbackData.Points.Four)
      ],
      [
        InlineKeyboardButton.WithCallbackData("⬅️ Назад", backCallBackData)
      ]
    ]);

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
