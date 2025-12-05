using System.Linq.Expressions;
using FamilyTaskManager.Core.SpotAggregate;

namespace FamilyTaskManager.Core.TaskAggregate.DTOs;

/// <summary>
///   DTO for task data with timezone support - used in query specifications
/// </summary>
public record TaskDto(
  Guid Id,
  string Title,
  TaskPoints Points,
  TaskStatus Status,
  DateTime DueAtUtc,
  Guid SpotId,
  string SpotName,
  Guid? StartedByUserId,
  string FamilyTimezone,
  string? StartedByUserName = null,
  SpotType SpotType = SpotType.OtherPet)
{
  /// <summary>
  ///   DueAt converted to family's local timezone
  /// </summary>
  public DateTime DueAtLocal
  {
    get
    {
      try
      {
        var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(FamilyTimezone);
        return TimeZoneInfo.ConvertTimeFromUtc(DueAtUtc, timeZoneInfo);
      }
      catch
      {
        // Fallback to UTC if timezone is invalid
        return DueAtUtc;
      }
    }
  }

  public static class Projections
  {
    public static readonly Expression<Func<TaskInstance, TaskDto>> FromTaskInstance = t => new TaskDto(
      t.Id,
      t.Title,
      t.Points,
      t.Status,
      t.DueAt,
      t.SpotId,
      t.Spot.Name,
      t.StartedByMember!.UserId,
      t.Family.Timezone,
      t.StartedByMember != null ? t.StartedByMember.User!.Name : null,
      t.Spot.Type);
  }
}
