using Telegram.Bot.Types.ReplyMarkups;

namespace FamilyTaskManager.Host.Modules.Bot.Helpers;

/// <summary>
///   Помощник для работы с главным меню бота
/// </summary>
public static class MainMenuHelper
{
  /// <summary>
  ///   Получить клавиатуру главного меню
  /// </summary>
  public static ReplyKeyboardMarkup GetMainMenuKeyboard() =>
    new(new[]
    {
      new KeyboardButton[] { "🏠 Семья", "✅ Мои задачи" },
      new KeyboardButton[] { "🐾 Питомец", "⭐ Мои очки" },
      new KeyboardButton[] { "📊 Статистика" }
    })
    {
      ResizeKeyboard = true,
      IsPersistent = true
    };
}
