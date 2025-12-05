using FamilyTaskManager.Core.SpotAggregate;
using FamilyTaskManager.Host.Modules.Bot.Constants;
using Telegram.Bot.Types.ReplyMarkups;

namespace FamilyTaskManager.Host.Modules.Bot.Helpers;

/// <summary>
///   Helper class for working with Spot types in the bot.
///   Delegates Spot type metadata to Core (SpotTypeDisplay) and contains only Telegram-specific helpers.
/// </summary>
public static class SpotTypeHelper
{
  public static string GetEmoji(SpotType spotType) =>
    SpotDisplay.GetEmoji(spotType);

  public static string GetDisplayText(SpotType spotType) =>
    SpotDisplay.GetDisplayText(spotType);

  public static (string emoji, string text) GetInfo(SpotType spotType) =>
    SpotDisplay.GetInfo(spotType);

  public static string GetEmojiFromString(string SpotTypeString) =>
    SpotDisplay.GetEmojiFromCode(SpotTypeString);

  /// <summary>
  ///   Creates inline keyboard buttons for Spot type selection.
  /// </summary>
  /// <param name="includeBackButton">Whether to include a back button at the end</param>
  public static InlineKeyboardButton[][] GetSpotTypeSelectionButtons(bool includeBackButton = false)
  {
    var buttons = new List<InlineKeyboardButton[]>();

    foreach (var SpotType in Enum.GetValues<SpotType>().Order())
    {
      var (emoji, text) = SpotDisplay.GetInfo(SpotType);

      // Callback data still uses the original lowercase code from Core
      var callbackCode = SpotType.ToString().ToLowerInvariant();

      buttons.Add(new[]
      {
        InlineKeyboardButton.WithCallbackData(
          $"{emoji} {text}",
          CallbackData.SpotType.Select(callbackCode))
      });
    }

    if (includeBackButton)
      buttons.Add(new[]
      {
        InlineKeyboardButton.WithCallbackData("⬅️ Назад", "Spot_back")
      });

    return buttons.ToArray();
  }
}
