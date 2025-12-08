using FamilyTaskManager.Core.TaskAggregate;
using FamilyTaskManager.Host.Modules.Bot.Constants;
using Telegram.Bot.Types.ReplyMarkups;

namespace FamilyTaskManager.Host.Modules.Bot.Helpers;

public static class TaskPointsHelper
{
  /// <summary>
  ///   Creates inline keyboard with star buttons for selecting task points.
  /// </summary>
  public static InlineKeyboardMarkup GetPointsSelectionKeyboard(string backCallBackData)
  {
    var pointButtons = TaskPoints.AllValues
      .Select(taskPoints => InlineKeyboardButton.WithCallbackData(
        taskPoints.ToStars(),
        CallbackData.TemplateForm.SelectPoints(taskPoints.Value)))
      .ToArray();

    return new InlineKeyboardMarkup([
      pointButtons,
      [InlineKeyboardButton.WithCallbackData("⬅️ Назад", backCallBackData)]
    ]);
  }

  /// <summary>
  ///   Converts points value to star representation.
  /// </summary>
  public static string ToStars(int points) =>
    TaskPoints.IsValidValue(points)
      ? new('⭐', points)
      : points.ToString();
}
