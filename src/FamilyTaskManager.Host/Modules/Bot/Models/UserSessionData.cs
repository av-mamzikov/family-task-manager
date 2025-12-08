namespace FamilyTaskManager.UnitTests.Host.Bot.Models;

public class UserSessionData
{
  public string? InternalState { get; set; }

  public string? FamilyName { get; set; }
  public string? Title { get; set; }
  public int? Points { get; set; }
  public Guid? SpotId { get; set; }
  public string? ScheduleType { get; set; }
  public TimeOnly? ScheduleTime { get; set; }
  public DayOfWeek? ScheduleDayOfWeek { get; set; }
  public int? ScheduleDayOfMonth { get; set; }
  public TimeSpan? DueDuration { get; set; }
  public Guid? TemplateId { get; set; }
  public string? SpotType { get; set; }
}
