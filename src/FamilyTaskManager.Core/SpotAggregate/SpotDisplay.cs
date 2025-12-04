namespace FamilyTaskManager.Core.SpotAggregate;

public static class SpotDisplay
{
  private static readonly Dictionary<SpotType, SpotTypeInfo> _spotTypeInfoMap = new()
  {
    { SpotType.Cat, new("🐱", "Кот", "cat") },
    { SpotType.Dog, new("🐶", "Собака", "dog") },
    { SpotType.Hamster, new("🐹", "Хомяк", "hamster") },
    { SpotType.Parrot, new("🦜", "Попугай", "parrot") }
  };

  public static string GetEmoji(SpotType spotType) =>
    _spotTypeInfoMap.TryGetValue(spotType, out var info) ? info.Emoji : "🐾";

  public static string GetDisplayText(SpotType spotType) =>
    _spotTypeInfoMap.TryGetValue(spotType, out var info) ? info.DisplayText : "Неизвестно";

  public static (string emoji, string text) GetInfo(SpotType spotType)
  {
    if (_spotTypeInfoMap.TryGetValue(spotType, out var info)) return (info.Emoji, info.DisplayText);

    return ("🐾", "Неизвестно");
  }

  public static (string emoji, string text) GetMoodInfo(int moodScore) =>
    moodScore switch
    {
      >= 80 => ("😊", "Отлично!"),
      >= 60 => ("🙂", "Хорошо"),
      >= 40 => ("😐", "Нормально"),
      >= 20 => ("😟", "Грустит"),
      _ => ("😢", "Очень грустно")
    };

  public static string GetEmojiFromCode(string spotTypeCode)
  {
    var info = _spotTypeInfoMap.Values.FirstOrDefault(i =>
      i.CallbackData.Equals(spotTypeCode, StringComparison.OrdinalIgnoreCase));
    return info?.Emoji ?? "🐾";
  }

  private record SpotTypeInfo(string Emoji, string DisplayText, string CallbackData);
}
