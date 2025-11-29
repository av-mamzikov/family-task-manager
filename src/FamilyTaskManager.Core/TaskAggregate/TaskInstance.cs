using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.PetAggregate;
using FamilyTaskManager.Core.TaskAggregate.Events;

namespace FamilyTaskManager.Core.TaskAggregate;

public class TaskInstance : EntityBase<TaskInstance, Guid>, IAggregateRoot
{
  private TaskInstance()
  {
  }

  public TaskInstance(Guid familyId, Guid petId, string title, TaskPoints points, TaskType type, DateTime dueAt,
    Guid? templateId = null)
  {
    Guard.Against.Default(familyId);
    Guard.Against.Default(petId);
    Guard.Against.NullOrWhiteSpace(title);
    Guard.Against.Null(points);

    FamilyId = familyId;
    PetId = petId;
    Title = title.Trim();
    Points = points;
    Type = type;
    TemplateId = templateId;
    Status = TaskStatus.Active;
    CreatedAt = DateTime.UtcNow;
    DueAt = dueAt;

    RegisterDomainEvent(new TaskCreatedEvent(this));
  }

  public Guid FamilyId { get; private set; }
  public Guid PetId { get; private set; }
  public string Title { get; private set; } = null!;
  public TaskPoints Points { get; private set; } = null!;
  public TaskType Type { get; private set; }
  public Guid? TemplateId { get; private set; }
  public TaskStatus Status { get; private set; }
  public Guid? StartedByMemberId { get; private set; }
  public Guid? CompletedByMemberId { get; private set; }
  public DateTime? CompletedAt { get; private set; }
  public DateTime CreatedAt { get; private set; }
  public DateTime DueAt { get; private set; }

  // Navigation properties
  public Family Family { get; private set; } = null!;
  public Pet Pet { get; private set; } = null!;
  public TaskTemplate? Template { get; private set; }
  public FamilyMember? StartedByMember { get; private set; }
  public FamilyMember? CompletedByMember { get; private set; }

  public void Start(Guid familyMemberId)
  {
    if (Status != TaskStatus.Active)
    {
      return;
    }

    Guard.Against.Default(familyMemberId);

    Status = TaskStatus.InProgress;
    StartedByMemberId = familyMemberId;
  }

  public void Complete(Guid completedByMemberId, DateTime completedAtUtc)
  {
    if (Status == TaskStatus.Completed)
    {
      return;
    }

    Guard.Against.Default(completedByMemberId);

    Status = TaskStatus.Completed;
    CompletedByMemberId = completedByMemberId;
    CompletedAt = completedAtUtc;

    RegisterDomainEvent(new TaskCompletedEvent(this));
  }

  /// <summary>
  ///   Triggers a reminder for this task
  /// </summary>
  public void TriggerReminder()
  {
    // Only send reminders for active or in-progress tasks
    if (Status != TaskStatus.Active && Status != TaskStatus.InProgress)
    {
      return;
    }

    RegisterDomainEvent(new TaskReminderDueEvent(this));
  }
}
