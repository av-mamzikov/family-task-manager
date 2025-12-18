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
  Guid? AssignedToUserId,
  string FamilyTimezone,
  string? AssignedToUserName = null,
  long? AssignedToUserTelegramId = null,
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
    public static readonly Expression<Func<TaskInstance, TaskDto>> FromTaskInstance = t => new(
      t.Id,
      t.Title,
      t.Points,
      t.Status,
      t.DueAt,
      t.SpotId,
      t.Spot.Name,
      t.AssignedToMember != null ? t.AssignedToMember.UserId : null,
      t.Family.Timezone,
      t.AssignedToMember != null ? t.AssignedToMember.User!.Name : null,
      t.AssignedToMember != null ? t.AssignedToMember.User!.TelegramId : null,
      t.Spot.Type);
  }
}
