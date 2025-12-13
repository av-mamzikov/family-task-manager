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
    Guid? templateId = null)
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

    RegisterDomainEvent(new TaskCreatedEvent
    {
      TaskId = Id,
      FamilyId = spot.FamilyId,
      SpotId = spot.Id,
      Title = title.Trim(),
      SpotName = spot.Name,
      Points = points.ToString(),
      DueAt = dueAt,
      Timezone = spot.Family.Timezone
    });
  }

  public Guid FamilyId { get; }
  public Guid SpotId { get; private set; }
  public string Title { get; } = null!;
  public TaskPoints Points { get; } = null!;
  public Guid? TemplateId { get; }
  public TaskStatus Status { get; private set; }
  public Guid? StartedByMemberId { get; private set; }
  public Guid? CompletedByMemberId { get; private set; }
  public DateTime? CompletedAt { get; private set; }
  public DateTime CreatedAt { get; private set; }

  public DateTime DueAt { get; }

  // Navigation properties
  public Family Family { get; } = null!;
  public Spot Spot { get; } = null!;
  public TaskTemplate? Template { get; private set; }
  public FamilyMember? StartedByMember { get; private set; }
  public FamilyMember? CompletedByMember { get; private set; }

  public void Start(FamilyMember familyMember)
  {
    if (Status != TaskStatus.Active) return;

    Guard.Against.Default(familyMember.Id);

    Status = TaskStatus.InProgress;
    StartedByMemberId = familyMember.Id;

    RegisterDomainEvent(new TaskStartedEvent { TaskId = Id });
  }

  public void Complete(FamilyMember completedByMember, DateTime completedAtUtc)
  {
    if (Status == TaskStatus.Completed) return;

    Guard.Against.Null(completedByMember);

    Status = TaskStatus.Completed;
    CompletedByMemberId = completedByMember.Id;
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
    StartedByMemberId = null;
  }

  /// <summary>
  ///   Deletes the task instance
  /// </summary>
  /// <param name="deletedByMember">The family member who is deleting the task (null for system deletion)</param>
  public void Delete(FamilyMember? deletedByMember = null) =>
    RegisterDomainEvent(new TaskDeletedEvent
    {
      TaskId = Id,
      FamilyId = FamilyId,
      Title = Title,
      Points = Points.ToString(),
      DeletedByUserId = deletedByMember?.UserId,
      DeletedByUserName = deletedByMember?.User.Name,
      DeletedByUserTelegramId = deletedByMember?.User.TelegramId,
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
      DueAt = DueAt,
      Timezone = Family.Timezone
    });
  }
}
