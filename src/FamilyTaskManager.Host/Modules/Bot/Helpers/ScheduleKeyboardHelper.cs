using FamilyTaskManager.Host.Modules.Bot.Constants;
using Telegram.Bot.Types.ReplyMarkups;

namespace FamilyTaskManager.Host.Modules.Bot.Helpers;

/// <summary>
///   Helper class for creating schedule-related keyboards.
/// </summary>
public static class ScheduleKeyboardHelper
{
  /// <summary>
  ///   Creates inline keyboard for schedule type selection.
  /// </summary>
  public static InlineKeyboardMarkup GetScheduleTypeKeyboard(string backCallbackData) =>
    new([
      [
        InlineKeyboardButton.WithCallbackData("📅 Ежедневно", CallbackData.Schedule.TypeDaily)
      ],
      [
        InlineKeyboardButton.WithCallbackData("💼 По будням (пн-пт)", CallbackData.Schedule.TypeWorkdays)
      ],
      [
        InlineKeyboardButton.WithCallbackData("🎉 По выходным (сб-вс)", CallbackData.Schedule.TypeWeekends)
      ],
      [
        InlineKeyboardButton.WithCallbackData("📆 Еженедельно", CallbackData.Schedule.TypeWeekly)
      ],
      [
        InlineKeyboardButton.WithCallbackData("🗓️ Ежемесячно", CallbackData.Schedule.TypeMonthly)
      ],
      [
        InlineKeyboardButton.WithCallbackData("✋ Вручную", CallbackData.Schedule.TypeManual)
      ],
      [
        InlineKeyboardButton.WithCallbackData("⬅️ Назад", backCallbackData)
      ]
    ]);

  /// <summary>
  ///   Creates inline keyboard for weekday selection.
  /// </summary>
  public static InlineKeyboardMarkup GetWeekdayKeyboard() =>
    new([
      [
        InlineKeyboardButton.WithCallbackData("Пн", CallbackData.Schedule.WeekdayMonday),
        InlineKeyboardButton.WithCallbackData("Вт", CallbackData.Schedule.WeekdayTuesday),
        InlineKeyboardButton.WithCallbackData("Ср", CallbackData.Schedule.WeekdayWednesday)
      ],
      [
        InlineKeyboardButton.WithCallbackData("Чт", CallbackData.Schedule.WeekdayThursday),
        InlineKeyboardButton.WithCallbackData("Пт", CallbackData.Schedule.WeekdayFriday)
      ],
      [
        InlineKeyboardButton.WithCallbackData("Сб", CallbackData.Schedule.WeekdaySaturday),
        InlineKeyboardButton.WithCallbackData("Вс", CallbackData.Schedule.WeekdaySunday)
      ]
    ]);

  /// <summary>
  ///   Parses weekday callback data to DayOfWeek.
  /// </summary>
  public static DayOfWeek? ParseWeekdayCallback(string callback) =>
    callback switch
    {
      "monday" => DayOfWeek.Monday,
      "tuesday" => DayOfWeek.Tuesday,
      "wednesday" => DayOfWeek.Wednesday,
      "thursday" => DayOfWeek.Thursday,
      "friday" => DayOfWeek.Friday,
      "saturday" => DayOfWeek.Saturday,
      "sunday" => DayOfWeek.Sunday,
      _ => null
    };

  /// <summary>
  ///   Gets user-friendly schedule type name.
  /// </summary>
  public static string GetScheduleTypeName(string scheduleType) =>
    scheduleType switch
    {
      "daily" => "Ежедневно",
      "workdays" => "По будням",
      "weekends" => "По выходным",
      "weekly" => "Еженедельно",
      "monthly" => "Ежемесячно",
      "manual" => "Вручную",
      _ => "Неизвестно"
    };

  /// <summary>
  ///   Gets user-friendly weekday name.
  /// </summary>
  public static string GetWeekdayName(DayOfWeek dayOfWeek) =>
    dayOfWeek switch
    {
      DayOfWeek.Monday => "Понедельник",
      DayOfWeek.Tuesday => "Вторник",
      DayOfWeek.Wednesday => "Среда",
      DayOfWeek.Thursday => "Четверг",
      DayOfWeek.Friday => "Пятница",
      DayOfWeek.Saturday => "Суббота",
      DayOfWeek.Sunday => "Воскресенье",
      _ => "Неизвестно"
    };
}
