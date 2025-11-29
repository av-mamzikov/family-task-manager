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
  public static InlineKeyboardMarkup GetScheduleTypeKeyboard() =>
    new(new[]
    {
      new[]
      {
        InlineKeyboardButton.WithCallbackData("📅 Ежедневно", "schedule_type_daily")
      },
      new[]
      {
        InlineKeyboardButton.WithCallbackData("💼 По будням (пн-пт)", "schedule_type_workdays")
      },
      new[]
      {
        InlineKeyboardButton.WithCallbackData("🎉 По выходным (сб-вс)", "schedule_type_weekends")
      },
      new[]
      {
        InlineKeyboardButton.WithCallbackData("📆 Еженедельно", "schedule_type_weekly")
      },
      new[]
      {
        InlineKeyboardButton.WithCallbackData("🗓️ Ежемесячно", "schedule_type_monthly")
      }
    });

  /// <summary>
  ///   Creates inline keyboard for weekday selection.
  /// </summary>
  public static InlineKeyboardMarkup GetWeekdayKeyboard() =>
    new(new[]
    {
      new[]
      {
        InlineKeyboardButton.WithCallbackData("Пн", "schedule_weekday_monday"),
        InlineKeyboardButton.WithCallbackData("Вт", "schedule_weekday_tuesday"),
        InlineKeyboardButton.WithCallbackData("Ср", "schedule_weekday_wednesday")
      },
      new[]
      {
        InlineKeyboardButton.WithCallbackData("Чт", "schedule_weekday_thursday"),
        InlineKeyboardButton.WithCallbackData("Пт", "schedule_weekday_friday")
      },
      new[]
      {
        InlineKeyboardButton.WithCallbackData("Сб", "schedule_weekday_saturday"),
        InlineKeyboardButton.WithCallbackData("Вс", "schedule_weekday_sunday")
      }
    });

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
