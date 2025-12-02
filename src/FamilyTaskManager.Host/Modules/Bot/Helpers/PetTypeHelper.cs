using FamilyTaskManager.Core.PetAggregate;
using Telegram.Bot.Types.ReplyMarkups;

namespace FamilyTaskManager.Host.Modules.Bot.Helpers;

/// <summary>
///   Helper class for working with pet types in the bot.
///   Delegates pet type metadata to Core (PetTypeDisplay) and contains only Telegram-specific helpers.
/// </summary>
public static class PetTypeHelper
{
  public static string GetEmoji(PetType petType) =>
    PetDisplay.GetEmoji(petType);

  public static string GetDisplayText(PetType petType) =>
    PetDisplay.GetDisplayText(petType);

  public static (string emoji, string text) GetInfo(PetType petType) =>
    PetDisplay.GetInfo(petType);

  public static string GetEmojiFromString(string petTypeString) =>
    PetDisplay.GetEmojiFromCode(petTypeString);

  /// <summary>
  ///   Creates inline keyboard buttons for pet type selection.
  /// </summary>
  /// <param name="includeBackButton">Whether to include a back button at the end</param>
  public static InlineKeyboardButton[][] GetPetTypeSelectionButtons(bool includeBackButton = false)
  {
    var buttons = new List<InlineKeyboardButton[]>();

    foreach (var petType in Enum.GetValues<PetType>().Order())
    {
      var (emoji, text) = PetDisplay.GetInfo(petType);

      // Callback data still uses the original lowercase code from Core
      var callbackCode = petType.ToString().ToLowerInvariant();

      buttons.Add(new[]
      {
        InlineKeyboardButton.WithCallbackData(
          $"{emoji} {text}",
          $"select_pettype_{callbackCode}")
      });
    }

    if (includeBackButton)
      buttons.Add(new[]
      {
        InlineKeyboardButton.WithCallbackData("⬅️ Назад", "pet_back")
      });

    return buttons.ToArray();
  }
}
