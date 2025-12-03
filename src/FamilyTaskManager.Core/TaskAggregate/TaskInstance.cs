using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.PetAggregate;
using FamilyTaskManager.Core.TaskAggregate.Events;

namespace FamilyTaskManager.Core.TaskAggregate;

public class TaskInstance : EntityBase<TaskInstance, Guid>, IAggregateRoot
{
  private TaskInstance()
  {
  }

  public TaskInstance(Pet pet, string title, TaskPoints points, TaskType type, DateTime dueAt,
    Guid? templateId = null)
  {
    Guard.Against.Null(pet);
    Guard.Against.NullOrWhiteSpace(title);
    Guard.Against.Null(points);

    FamilyId = pet.FamilyId;
    PetId = pet.Id;
    Title = title.Trim();
    Points = points;
    Type = type;
    TemplateId = templateId;
    Status = TaskStatus.Active;
    CreatedAt = DateTime.UtcNow;
    DueAt = dueAt;

    RegisterDomainEvent(new TaskCreatedEvent
    {
      TaskId = Id,
      FamilyId = pet.FamilyId,
      PetId = pet.Id,
      Title = title.Trim(),
      PetName = pet.Name,
      Points = points.ToString(),
      DueAt = dueAt,
      Timezone = pet.Family.Timezone
    });
  }

  public Guid FamilyId { get; }
  public Guid PetId { get; private set; }
  public string Title { get; } = null!;
  public TaskPoints Points { get; } = null!;
  public TaskType Type { get; private set; }
  public Guid? TemplateId { get; private set; }
  public TaskStatus Status { get; private set; }
  public Guid? StartedByMemberId { get; private set; }
  public Guid? CompletedByMemberId { get; private set; }
  public DateTime? CompletedAt { get; private set; }
  public DateTime CreatedAt { get; private set; }
  public DateTime DueAt { get; }

  // Navigation properties
  public Family Family { get; } = null!;
  public Pet Pet { get; private set; } = null!;
  public TaskTemplate? Template { get; private set; }
  public FamilyMember? StartedByMember { get; private set; }
  public FamilyMember? CompletedByMember { get; private set; }

  public void Start(Guid familyMemberId)
  {
    if (Status != TaskStatus.Active) return;

    Guard.Against.Default(familyMemberId);

    Status = TaskStatus.InProgress;
    StartedByMemberId = familyMemberId;

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
      CompletedByUserName = completedByMember.User.Name
    });
  }

  public void Release()
  {
    if (Status != TaskStatus.InProgress) return;

    Status = TaskStatus.Active;
    StartedByMemberId = null;
  }

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
      Title = Title,
      DueAt = DueAt,
      Timezone = Family.Timezone
    });
  }
}
