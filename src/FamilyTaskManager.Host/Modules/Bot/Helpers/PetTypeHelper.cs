using FamilyTaskManager.Core.PetAggregate;
using Telegram.Bot.Types.ReplyMarkups;

namespace FamilyTaskManager.Host.Modules.Bot.Helpers;

/// <summary>
///   Helper class for working with pet types in the bot.
///   Centralizes pet type information to avoid code duplication.
/// </summary>
public static class PetTypeHelper
{
  private static readonly Dictionary<PetType, PetTypeInfo> PetTypeInfoMap = new()
  {
    { PetType.Cat, new PetTypeInfo("🐱", "Кот", "cat") },
    { PetType.Dog, new PetTypeInfo("🐶", "Собака", "dog") },
    { PetType.Hamster, new PetTypeInfo("🐹", "Хомяк", "hamster") },
    { PetType.Parrot, new PetTypeInfo("🦜", "Попугай", "parrot") }
  };

  /// <summary>
  ///   Gets the emoji for a pet type.
  /// </summary>
  public static string GetEmoji(PetType petType) =>
    PetTypeInfoMap.TryGetValue(petType, out var info) ? info.Emoji : "🐾";

  /// <summary>
  ///   Gets the display text for a pet type.
  /// </summary>
  public static string GetDisplayText(PetType petType) =>
    PetTypeInfoMap.TryGetValue(petType, out var info) ? info.DisplayText : "Неизвестно";

  /// <summary>
  ///   Gets both emoji and display text for a pet type.
  /// </summary>
  public static (string emoji, string text) GetInfo(PetType petType)
  {
    if (PetTypeInfoMap.TryGetValue(petType, out var info)) return (info.Emoji, info.DisplayText);

    return ("🐾", "Неизвестно");
  }

  /// <summary>
  ///   Gets the emoji for a pet type string (used in callbacks).
  /// </summary>
  public static string GetEmojiFromString(string petTypeString)
  {
    var info = PetTypeInfoMap.Values.FirstOrDefault(i =>
      i.CallbackData.Equals(petTypeString, StringComparison.OrdinalIgnoreCase));
    return info?.Emoji ?? "🐾";
  }

  /// <summary>
  ///   Creates inline keyboard buttons for pet type selection.
  /// </summary>
  /// <param name="includeBackButton">Whether to include a back button at the end</param>
  public static InlineKeyboardButton[][] GetPetTypeSelectionButtons(bool includeBackButton = false)
  {
    var buttons = new List<InlineKeyboardButton[]>();

    foreach (var (petType, info) in PetTypeInfoMap.OrderBy(x => x.Key))
      buttons.Add(new[]
      {
        InlineKeyboardButton.WithCallbackData($"{info.Emoji} {info.DisplayText}", $"select_pettype_{info.CallbackData}")
      });

    if (includeBackButton)
      buttons.Add(new[]
      {
        InlineKeyboardButton.WithCallbackData("⬅️ Назад", "pet_back")
      });

    return buttons.ToArray();
  }

  private record PetTypeInfo(string Emoji, string DisplayText, string CallbackData);
}
