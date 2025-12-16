using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.SpotAggregate;

namespace FamilyTaskManager.Core.TaskAggregate;

public class TaskTemplate : EntityBase<TaskTemplate, Guid>, IAggregateRoot
{
  private readonly List<FamilyMember> _responsibleMembers = [];

  private TaskTemplate()
  {
  }

  public TaskTemplate(Guid familyId, Guid spotId, TaskTitle title, TaskPoints points, Schedule schedule,
    DueDuration dueDuration,
    Guid createdBy)
  {
    Guard.Against.Default(familyId);
    Guard.Against.Default(spotId);
    Guard.Against.Default(createdBy);
    Guard.Against.Null(title);
    Guard.Against.Null(points);
    Guard.Against.Null(schedule);
    Guard.Against.Null(dueDuration);

    FamilyId = familyId;
    SpotId = spotId;
    Title = title;
    Points = points;
    Schedule = schedule;
    DueDuration = dueDuration;
    CreatedBy = createdBy;
    CreatedAt = DateTime.UtcNow;
  }

  public Guid FamilyId { get; private set; }
  public Family Family { get; private set; } = null!;
  public Guid SpotId { get; private set; }
  public Spot Spot { get; private set; } = null!;
  public TaskTitle Title { get; private set; } = null!;
  public TaskPoints Points { get; private set; } = null!;
  public Schedule Schedule { get; private set; } = null!;
  public DueDuration DueDuration { get; private set; } = null!;
  public Guid CreatedBy { get; private set; }
  public DateTime CreatedAt { get; private set; }

  public IReadOnlyCollection<FamilyMember> ResponsibleMembers => _responsibleMembers.AsReadOnly();

  public void Update(TaskTitle title, TaskPoints points, Schedule schedule, DueDuration dueDuration)
  {
    Guard.Against.Null(title);
    Guard.Against.Null(points);
    Guard.Against.Null(schedule);
    Guard.Against.Null(dueDuration);

    Title = title;
    Points = points;
    Schedule = schedule;
    DueDuration = dueDuration;
  }

  public void AssignResponsible(FamilyMember member)
  {
    Guard.Against.Null(member);

    if (member.FamilyId != FamilyId)
      throw new ArgumentException("Family member must belong to the same family as the TaskTemplate.", nameof(member));

    if (!member.IsActive)
      throw new InvalidOperationException("Inactive family member cannot be assigned as responsible.");

    if (_responsibleMembers.Any(m => m.Id == member.Id))
      return;

    _responsibleMembers.Add(member);
  }

  public void RemoveResponsible(FamilyMember member)
  {
    Guard.Against.Null(member);

    var existing = _responsibleMembers.FirstOrDefault(m => m.Id == member.Id);
    if (existing is null)
      return;

    _responsibleMembers.Remove(existing);
  }

  public void ClearAllResponsible()
  {
    _responsibleMembers.Clear();
  }
}

