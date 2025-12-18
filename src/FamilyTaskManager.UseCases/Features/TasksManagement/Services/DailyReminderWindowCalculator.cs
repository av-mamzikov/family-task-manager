namespace FamilyTaskManager.UseCases.Features.TasksManagement.Services;

public static class DailyReminderWindowCalculator
{
  public static bool CrossedLocalTimeBetween(
    DateTime? previousFireTimeUtc,
    DateTime currentFireTimeUtc,
    string timezone,
    ITimeZoneService timeZoneService,
    int targetHour,
    int targetMinute = 0)
  {
    if (previousFireTimeUtc is null)
      return false;

    var prevLocal = timeZoneService.ConvertFromUtc(previousFireTimeUtc.Value, timezone);
    var currentLocal = timeZoneService.ConvertFromUtc(currentFireTimeUtc, timezone);

    if (currentLocal <= prevLocal)
      return false;

    var candidateDate = currentLocal.Date;
    var target = candidateDate.AddHours(targetHour).AddMinutes(targetMinute);

    if (target > currentLocal)
      target = candidateDate.AddDays(-1).AddHours(targetHour).AddMinutes(targetMinute);

    return target > prevLocal && target <= currentLocal;
  }
}
