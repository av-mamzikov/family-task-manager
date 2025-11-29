namespace FamilyTaskManager.Core.TaskAggregate;

/// <summary>
///   Defines the type of schedule for a task template.
/// </summary>
public sealed class ScheduleType : SmartEnum<ScheduleType>
{
  public static readonly ScheduleType Daily = new(nameof(Daily), 1);
  public static readonly ScheduleType Weekly = new(nameof(Weekly), 2);
  public static readonly ScheduleType Monthly = new(nameof(Monthly), 3);
  public static readonly ScheduleType Workdays = new(nameof(Workdays), 4);
  public static readonly ScheduleType Weekends = new(nameof(Weekends), 5);

  private ScheduleType(string name, int value) : base(name, value)
  {
  }
}
