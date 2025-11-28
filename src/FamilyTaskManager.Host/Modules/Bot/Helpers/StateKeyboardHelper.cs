using FamilyTaskManager.Host.Modules.Bot.Models;
using Telegram.Bot.Types.ReplyMarkups;

namespace FamilyTaskManager.Host.Modules.Bot.Helpers;

/// <summary>
///   Помощник для генерации клавиатур с доступными действиями для каждого ConversationState
/// </summary>
public static class StateKeyboardHelper
{
  /// <summary>
  ///   Получить клавиатуру с доступными действиями для текущего состояния
  /// </summary>
  public static IReplyMarkup? GetKeyboardForState(ConversationState state) =>
    state switch
    {
      ConversationState.AwaitingFamilyTimezone => null, // Используются inline кнопки
      ConversationState.AwaitingFamilyLocation => GetLocationOrBackKeyboard(),
      ConversationState.AwaitingPetName => GetCancelKeyboard(),
      ConversationState.AwaitingTaskTitle => GetCancelKeyboard(),
      ConversationState.AwaitingTaskPoints => GetBackOrCancelKeyboard(),
      ConversationState.AwaitingTaskPetSelection => null, // Используются inline кнопки
      ConversationState.AwaitingTaskSchedule => GetBackOrCancelKeyboard(),
      ConversationState.AwaitingTaskDueDate => GetBackOrCancelKeyboard(),
      ConversationState.AwaitingTemplateTitle => GetCancelKeyboard(),
      ConversationState.AwaitingTemplatePoints => GetBackOrCancelKeyboard(),
      ConversationState.AwaitingTemplateSchedule => GetBackOrCancelKeyboard(),
      ConversationState.AwaitingTemplatePetSelection => null, // Используются inline кнопки
      ConversationState.AwaitingTemplateEditTitle => GetBackOrCancelKeyboard(),
      ConversationState.AwaitingTemplateEditPoints => GetBackOrCancelKeyboard(),
      ConversationState.AwaitingTemplateEditSchedule => GetBackOrCancelKeyboard(),
      ConversationState.AwaitingTemplateEditDueDuration => GetBackOrCancelKeyboard(),
      _ => null
    };

  /// <summary>
  ///   Получить текст подсказки с доступными действиями для состояния
  /// </summary>
  public static string GetHintForState(ConversationState state) =>
    state switch
    {
      ConversationState.AwaitingFamilyLocation =>
        "\n\n💡 Доступные действия:\n• Отправьте геолокацию\n• ⬅️ Назад - Вернуться к выбору способа",

      ConversationState.AwaitingPetName =>
        "\n\n💡 Доступные действия:\n• Введите имя питомца (2-50 символов)\n• /cancel - Отменить создание",

      ConversationState.AwaitingTaskTitle =>
        "\n\n💡 Доступные действия:\n• Введите название задачи (3-100 символов)\n• /cancel - Отменить создание",

      ConversationState.AwaitingTaskPoints =>
        "\n\n💡 Доступные действия:\n• Введите количество очков (1-100)\n• ⬅️ Назад - К названию\n• /cancel - Отменить создание",

      ConversationState.AwaitingTaskSchedule =>
        "\n\n💡 Доступные действия:\n• Введите расписание в формате Cron\n• ⬅️ Назад - К выбору питомца\n• /cancel - Отменить создание",

      ConversationState.AwaitingTaskDueDate =>
        "\n\n💡 Доступные действия:\n• Введите срок в днях (0-365)\n• ⬅️ Назад - К выбору питомца\n• /cancel - Отменить создание",

      ConversationState.AwaitingTemplateTitle =>
        "\n\n💡 Доступные действия:\n• Введите название шаблона (3-100 символов)\n• /cancel - Отменить создание",

      ConversationState.AwaitingTemplatePoints =>
        "\n\n💡 Доступные действия:\n• Введите количество очков (1-100)\n• ⬅️ Назад - К названию\n• /cancel - Отменить создание",

      ConversationState.AwaitingTemplateSchedule =>
        "\n\n💡 Доступные действия:\n• Введите расписание в формате Cron\n• ⬅️ Назад - К очкам\n• /cancel - Отменить создание",

      ConversationState.AwaitingTemplateEditTitle =>
        "\n\n💡 Доступные действия:\n• Введите новое название\n• ⬅️ Назад - Отменить редактирование\n• /cancel - Выйти из режима редактирования",

      ConversationState.AwaitingTemplateEditPoints =>
        "\n\n💡 Доступные действия:\n• Введите новое количество очков\n• ⬅️ Назад - Отменить редактирование\n• /cancel - Выйти из режима редактирования",

      ConversationState.AwaitingTemplateEditSchedule =>
        "\n\n💡 Доступные действия:\n• Введите новое расписание\n• ⬅️ Назад - Отменить редактирование\n• /cancel - Выйти из режима редактирования",

      ConversationState.AwaitingTemplateEditDueDuration =>
        "\n\n💡 Доступные действия:\n• Введите новый срок выполнения в часах (0-24)\n• ⬅️ Назад - Отменить редактирование\n• /cancel - Выйти из режима редактирования",

      _ => ""
    };

  private static ReplyKeyboardMarkup GetCancelKeyboard() =>
    new(new[]
    {
      new KeyboardButton[] { new("❌ Отменить") }
    })
    {
      ResizeKeyboard = true,
      OneTimeKeyboard = false
    };

  private static ReplyKeyboardMarkup GetBackOrCancelKeyboard() =>
    new(new[]
    {
      new KeyboardButton[] { new("⬅️ Назад"), new("❌ Отменить") }
    })
    {
      ResizeKeyboard = true,
      OneTimeKeyboard = false
    };

  private static ReplyKeyboardMarkup GetLocationOrBackKeyboard() =>
    new(new[]
    {
      new KeyboardButton[] { new("📍 Отправить местоположение") { RequestLocation = true } },
      new KeyboardButton[] { new("⬅️ Назад") }
    })
    {
      ResizeKeyboard = true,
      OneTimeKeyboard = false
    };
}
