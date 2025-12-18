using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.SpotAggregate;
using FamilyTaskManager.Core.TaskAggregate.Events;

namespace FamilyTaskManager.Core.TaskAggregate;

public class TaskInstance : EntityBase<TaskInstance, Guid>, IAggregateRoot
{
  private TaskInstance()
  {
  }

  public TaskInstance(Spot spot, string title, TaskPoints points, DateTime dueAt,
    Guid? templateId = null,
    FamilyMember? assignedToMember = null)
  {
    Guard.Against.Null(spot);
    Guard.Against.NullOrWhiteSpace(title);
    Guard.Against.Null(points);

    Id = Guid.NewGuid();
    FamilyId = spot.FamilyId;
    SpotId = spot.Id;
    Title = title.Trim();
    Points = points;
    TemplateId = templateId;
    Status = TaskStatus.Active;
    CreatedAt = DateTime.UtcNow;
    DueAt = dueAt;
    AssignedToMember = assignedToMember;
    AssignedToMemberId = assignedToMember?.Id;

    RegisterDomainEvent(new TaskCreatedEvent
    {
      TaskId = Id,
      FamilyId = spot.FamilyId,
      SpotId = spot.Id,
      Title = title.Trim(),
      SpotName = spot.Name,
      Points = points.ToString(),
      DueAt = dueAt,
      Timezone = spot.Family.Timezone,
      AssignedUserName = assignedToMember?.User?.Name,
      AssignedUserTelegramId = assignedToMember?.User?.TelegramId
    });
  }

  public Guid FamilyId { get; }
  public Guid SpotId { get; private set; }
  public string Title { get; } = null!;
  public TaskPoints Points { get; } = null!;
  public Guid? TemplateId { get; }
  public TaskStatus Status { get; private set; }
  public Guid? AssignedToMemberId { get; private set; }
  public DateTime? CompletedAt { get; private set; }
  public DateTime CreatedAt { get; private set; }

  public DateTime DueAt { get; }

  // Navigation properties
  public Family Family { get; } = null!;
  public Spot Spot { get; } = null!;
  public TaskTemplate? Template { get; private set; }
  public FamilyMember? AssignedToMember { get; private set; }

  public Result AssignToMember(FamilyMember familyMember)
  {
    Guard.Against.Null(familyMember);
    Guard.Against.Default(familyMember.Id);

    if (!familyMember.IsActive) return Result.Error("Inactive family member cannot be assigned.");
    if (FamilyId != familyMember.FamilyId) return Result.Error("User is not a member of this family");

    AssignedToMemberId = familyMember.Id;
    AssignedToMember = familyMember;

    return Result.Success();
  }

  public Result StartByUserId(Guid userId, Family family)
  {
    var member = family.Members.FirstOrDefault(m => m.UserId == userId && m.IsActive);
    return member == null
      ? Result.Error("User is not a member of this family")
      : StartByMember(member);
  }

  public Result StartByMember(FamilyMember familyMember)
  {
    Guard.Against.Default(familyMember.Id);
    if (Status != TaskStatus.Active) return Result.Error("Task is not available");
    if (FamilyId != familyMember.FamilyId) return Result.Error("User is not a member of this family");

    Status = TaskStatus.InProgress;
    AssignedToMemberId = familyMember.Id;
    AssignedToMember = familyMember;

    RegisterDomainEvent(new TaskStartedEvent { TaskId = Id });

    return Result.Success();
  }

  public void Complete(FamilyMember completedByMember, DateTime completedAtUtc)
  {
    if (Status == TaskStatus.Completed) return;

    Guard.Against.Null(completedByMember);

    Status = TaskStatus.Completed;
    AssignedToMemberId = completedByMember.Id;
    AssignedToMember = completedByMember;
    CompletedAt = completedAtUtc;

    RegisterDomainEvent(new TaskCompletedEvent
    {
      TaskId = Id,
      FamilyId = FamilyId,
      Title = Title,
      Points = Points.ToString(),
      CompletedByUserId = completedByMember.UserId,
      CompletedByUserName = completedByMember.User.Name,
      CompletedByUserTelegramId = completedByMember.User.TelegramId
    });
  }

  public void Release()
  {
    if (Status != TaskStatus.InProgress) return;

    Status = TaskStatus.Active;
    AssignedToMemberId = null;
    AssignedToMember = null;
  }

  /// <summary>
  ///   Deletes the task instance
  /// </summary>
  /// <param name="deletedByMember">The family member who is deleting the task (null for system deletion)</param>
  public void Delete(FamilyMember deletedByMember) =>
    RegisterDomainEvent(new TaskDeletedEvent
    {
      TaskId = Id,
      FamilyId = FamilyId,
      Title = Title,
      Points = Points.ToString(),
      DeletedByUserId = deletedByMember.UserId,
      DeletedByUserName = deletedByMember.User.Name,
      DeletedByUserTelegramId = deletedByMember.User.TelegramId,
      SpotName = Spot.Name,
      SpotType = Spot.Type.ToString()
    });

  /// <summary>
  ///   Triggers a reminder for this task
  /// </summary>
  public void TriggerReminder()
  {
    // Only send reminders for active or in-progress tasks
    if (Status != TaskStatus.Active && Status != TaskStatus.InProgress) return;

    RegisterDomainEvent(new TaskReminderDueEvent
    {
      TaskId = Id,
      FamilyId = FamilyId,
      TemplateId = TemplateId,
      Title = Title,
      SpotName = Spot.Name,
      DueAt = DueAt,
      Timezone = Family.Timezone,
      AssignedUserName = AssignedToMember?.User?.Name,
      AssignedUserTelegramId = AssignedToMember?.User?.TelegramId
    });
  }
}
