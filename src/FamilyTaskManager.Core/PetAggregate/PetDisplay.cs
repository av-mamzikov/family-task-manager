namespace FamilyTaskManager.Core.PetAggregate;

public static class PetDisplay
{
  private static readonly Dictionary<PetType, PetTypeInfo> _petTypeInfoMap = new()
  {
    { PetType.Cat, new("🐱", "Кот", "cat") },
    { PetType.Dog, new("🐶", "Собака", "dog") },
    { PetType.Hamster, new("🐹", "Хомяк", "hamster") },
    { PetType.Parrot, new("🦜", "Попугай", "parrot") }
  };

  public static string GetEmoji(PetType petType) =>
    _petTypeInfoMap.TryGetValue(petType, out var info) ? info.Emoji : "🐾";

  public static string GetDisplayText(PetType petType) =>
    _petTypeInfoMap.TryGetValue(petType, out var info) ? info.DisplayText : "Неизвестно";

  public static (string emoji, string text) GetInfo(PetType petType)
  {
    if (_petTypeInfoMap.TryGetValue(petType, out var info)) return (info.Emoji, info.DisplayText);

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

  public static string GetEmojiFromCode(string petTypeCode)
  {
    var info = _petTypeInfoMap.Values.FirstOrDefault(i =>
      i.CallbackData.Equals(petTypeCode, StringComparison.OrdinalIgnoreCase));
    return info?.Emoji ?? "🐾";
  }

  private record PetTypeInfo(string Emoji, string DisplayText, string CallbackData);
}
