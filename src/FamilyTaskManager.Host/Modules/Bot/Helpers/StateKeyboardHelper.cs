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
      ConversationState.AwaitingTaskPoints => null, // Используются inline кнопки
      ConversationState.AwaitingTaskPetSelection => null, // Используются inline кнопки
      ConversationState.AwaitingTaskSchedule => GetBackOrCancelKeyboard(),
      ConversationState.AwaitingTaskDueDate => GetBackOrCancelKeyboard(),
      ConversationState.AwaitingTemplateTitle => GetCancelKeyboard(),
      ConversationState.AwaitingTemplatePoints => null, // Используются inline кнопки
      ConversationState.AwaitingTemplateScheduleType => null, // Используются inline кнопки
      ConversationState.AwaitingTemplateScheduleTime => GetBackOrCancelKeyboard(),
      ConversationState.AwaitingTemplateScheduleWeekday => null, // Используются inline кнопки
      ConversationState.AwaitingTemplateScheduleMonthDay => GetBackOrCancelKeyboard(),
      ConversationState.AwaitingTemplateDueDuration => GetBackOrCancelKeyboard(),
      ConversationState.AwaitingTemplatePetSelection => null, // Используются inline кнопки
      ConversationState.AwaitingTemplateEditTitle => GetBackOrCancelKeyboard(),
      ConversationState.AwaitingTemplateEditPoints => null, // Используются inline кнопки
      ConversationState.AwaitingTemplateEditScheduleType => null, // Используются inline кнопки
      ConversationState.AwaitingTemplateEditScheduleTime => GetBackOrCancelKeyboard(),
      ConversationState.AwaitingTemplateEditScheduleWeekday => null, // Используются inline кнопки
      ConversationState.AwaitingTemplateEditScheduleMonthDay => GetBackOrCancelKeyboard(),
      ConversationState.AwaitingTemplateEditDueDuration => GetBackOrCancelKeyboard(),
      ConversationState.AwaitingTaskScheduleType => null, // Используются inline кнопки
      ConversationState.AwaitingTaskScheduleTime => GetBackOrCancelKeyboard(),
      ConversationState.AwaitingTaskScheduleWeekday => null, // Используются inline кнопки
      ConversationState.AwaitingTaskScheduleMonthDay => GetBackOrCancelKeyboard(),
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
        "\n\n💡 Доступные действия:\n• Введите имя питомца (2-50 символов)\n•",

      ConversationState.AwaitingTaskTitle =>
        "\n\n💡 Доступные действия:\n• Введите название задачи (3-100 символов)\n",

      ConversationState.AwaitingTaskPoints =>
        "\n\n💡 Выберите сложность с помощью кнопок выше",

      ConversationState.AwaitingTaskScheduleType =>
        "\n\n💡 Выберите тип расписания с помощью кнопок выше",

      ConversationState.AwaitingTaskScheduleTime =>
        "\n\n💡 Доступные действия:\n• Введите время (например, 09:00)\n• ⬅️ Назад - К типу расписания\n",

      ConversationState.AwaitingTaskScheduleWeekday =>
        "\n\n💡 Выберите день недели с помощью кнопок выше",

      ConversationState.AwaitingTaskScheduleMonthDay =>
        "\n\n💡 Доступные действия:\n• Введите день месяца (1-31)\n• ⬅️ Назад - К типу расписания\n",

      ConversationState.AwaitingTaskSchedule =>
        "\n\n💡 Доступные действия:\n• Введите расписание в формате Cron\n• ⬅️ Назад - К выбору питомца\n",

      ConversationState.AwaitingTaskDueDate =>
        "\n\n💡 Доступные действия:\n• Введите срок в днях (0-365)\n• ⬅️ Назад - К выбору питомца\n",

      ConversationState.AwaitingTemplateTitle =>
        "\n\n💡 Доступные действия:\n• Введите название шаблона (3-100 символов)\n",

      ConversationState.AwaitingTemplatePoints =>
        "\n\n💡 Выберите сложность с помощью кнопок выше",

      ConversationState.AwaitingTemplateScheduleType =>
        "\n\n💡 Выберите тип расписания с помощью кнопок выше",

      ConversationState.AwaitingTemplateScheduleTime =>
        "\n\n💡 Доступные действия:\n• Введите время (например, 09:00)\n• ⬅️ Назад - К типу расписания\n",

      ConversationState.AwaitingTemplateScheduleWeekday =>
        "\n\n💡 Выберите день недели с помощью кнопок выше",

      ConversationState.AwaitingTemplateScheduleMonthDay =>
        "\n\n💡 Доступные действия:\n• Введите день месяца (1-31)\n• ⬅️ Назад - К типу расписания\n",

      ConversationState.AwaitingTemplateDueDuration =>
        "\n\n💡 Доступные действия:\n• Введите срок выполнения в часах (0-24)\n• ⬅️ Назад - К расписанию\n",

      ConversationState.AwaitingTemplateEditTitle =>
        "\n\n💡 Доступные действия:\n• Введите новое название\n• ⬅️ Назад - Отменить редактирование\n• /cancel - Выйти из режима редактирования",

      ConversationState.AwaitingTemplateEditPoints =>
        "\n\n💡 Выберите новую сложность с помощью кнопок выше",

      ConversationState.AwaitingTemplateEditScheduleType =>
        "\n\n💡 Выберите новый тип расписания с помощью кнопок выше",

      ConversationState.AwaitingTemplateEditScheduleTime =>
        "\n\n💡 Доступные действия:\n• Введите новое время\n• ⬅️ Назад - Отменить редактирование\n• /cancel - Выйти из режима редактирования",

      ConversationState.AwaitingTemplateEditScheduleWeekday =>
        "\n\n💡 Выберите новый день недели с помощью кнопок выше",

      ConversationState.AwaitingTemplateEditScheduleMonthDay =>
        "\n\n💡 Доступные действия:\n• Введите новый день месяца\n• ⬅️ Назад - Отменить редактирование\n• /cancel - Выйти из режима редактирования",

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
