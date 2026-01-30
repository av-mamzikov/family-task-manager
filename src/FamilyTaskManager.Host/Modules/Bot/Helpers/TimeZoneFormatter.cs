using TimeZones = FamilyTaskManager.Host.Modules.Bot.Constants.TimeZones;

namespace FamilyTaskManager.Host.Modules.Bot.Helpers;

public static class TimeZoneFormatter
{
  private static readonly TimeZoneInfo MoscowTimeZone = TimeZoneInfo.FindSystemTimeZoneById(TimeZones.Russian.Moscow);


  public static string FormatTimezoneWithTime(string timezoneId)
  {
    var timeZone = TimeZoneInfo.TryFindSystemTimeZoneById(timezoneId, out var timeZoneInfo) ? timeZoneInfo : null;
    if (timeZone == null)
      return "Неизвестно";

    var offsetLabel = GetMoscowOffsetLabel(timeZone);
    var timeInZone = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone).ToString("HH:mm");
    return $"{offsetLabel} (сейчас {timeInZone}) {TimeZones.Russian.GetName(timezoneId)}";
  }

  public static string GetMoscowOffsetLabel(TimeZoneInfo targetTimeZone)
  {
    var now = DateTime.UtcNow;
    var moscowOffset = MoscowTimeZone.GetUtcOffset(now);
    var targetOffset = targetTimeZone.GetUtcOffset(now);
    var difference = targetOffset - moscowOffset;
    if (difference == TimeSpan.Zero)
      return "МСК";

    var sign = difference.TotalHours >= 0 ? "+" : "-";
    var absDifference = difference.Duration();
    return $"МСК {sign}{absDifference.Hours:D2}:{absDifference.Minutes:D2}";
  }
}
