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
  string? AssignedToUserName,
  long? AssignedToUserTelegramId,
  Guid FamilyId,
  string FamilyTimezone,
  SpotType SpotType)
{
  private DateTime? _dueAtLocal;

  /// <summary>
  ///   DueAt converted to family's local timezone
  /// </summary>
  public DateTime DueAtLocal
  {
    get
    {
      if (_dueAtLocal is not null)
        return _dueAtLocal.Value;
      try
      {
        var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(FamilyTimezone);
        _dueAtLocal = TimeZoneInfo.ConvertTimeFromUtc(DueAtUtc, timeZoneInfo);
        return _dueAtLocal.Value;
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
      t.AssignedToMember != null ? t.AssignedToMember.User!.Name : null,
      t.AssignedToMember != null ? t.AssignedToMember.User!.TelegramId : null,
      t.FamilyId,
      t.Family.Timezone,
      t.Spot.Type);
  }
}
