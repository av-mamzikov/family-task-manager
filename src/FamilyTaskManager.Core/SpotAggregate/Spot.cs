using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.SpotAggregate.Events;

namespace FamilyTaskManager.Core.SpotAggregate;

public enum SpotType
{
  Cat = 0,
  Dog = 1,
  Hamster = 2,
  Parrot = 3,

  Fish = 4,
  Turtle = 5,
  Plant = 6,

  OtherPet = 10,

  Kitchen = 20,
  Bathroom = 21,
  KidsRoom = 22,
  Hallway = 23,

  WashingMachine = 30,
  Dishwasher = 31,
  Fridge = 32,

  Finances = 40,
  Documents = 41
}

public class Spot : EntityBase<Spot, Guid>, IAggregateRoot
{
  private readonly List<FamilyMember> _responsibleMembers = [];

  private Spot()
  {
  }

  public Spot(Guid familyId, SpotType type, string name)
  {
    Guard.Against.Default(familyId);
    Guard.Against.NullOrWhiteSpace(name);

    Id = Guid.NewGuid();
    FamilyId = familyId;
    Type = type;
    Name = name.Trim();
    MoodScore = 100;
    CreatedAt = DateTime.UtcNow;

    RegisterDomainEvent(new SpotCreatedEvent
    {
      SpotId = Id,
      FamilyId = familyId,
      Name = name.Trim(),
      Type = type.ToString()
    });
  }

  public Guid FamilyId { get; }
  public SpotType Type { get; private set; }
  public string Name { get; private set; } = null!;

  // Navigation property
  public Family Family { get; private set; } = null!;

  public IReadOnlyCollection<FamilyMember> ResponsibleMembers => _responsibleMembers.AsReadOnly();

  /// <summary>
  ///   Денормализованное поле, которое рассчитывается периодически в зависимости от наличия невыполненных задач
  /// </summary>
  public int MoodScore { get; private set; }

  public DateTime CreatedAt { get; private set; }

  public bool IsDeleted { get; private set; }

  public void UpdateName(string name)
  {
    Guard.Against.NullOrWhiteSpace(name);
    Name = name.Trim();
  }

  public void UpdateMoodScore(int moodScore)
  {
    var oldMood = MoodScore;
    var newMood = Math.Clamp(moodScore, 0, 100);

    MoodScore = newMood;

    // Register event if mood change is significant
    if (ShouldNotifyMoodChange(oldMood, newMood))
      RegisterDomainEvent(new SpotMoodChangedEvent
      {
        SpotId = Id,
        FamilyId = FamilyId,
        Name = Name,
        OldMoodScore = oldMood,
        NewMoodScore = newMood
      });
  }

  public void AssignResponsible(FamilyMember member)
  {
    Guard.Against.Null(member);

    if (member.FamilyId != FamilyId)
      throw new ArgumentException("Family member must belong to the same family as the Spot.", nameof(member));

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

  public void SoftDelete()
  {
    if (IsDeleted)
      return;
    IsDeleted = true;
    RegisterDomainEvent(new SpotDeletedEvent
    {
      SpotId = Id,
      FamilyId = FamilyId,
      Name = Name
    });
  }

  /// <summary>
  ///   Determines if a mood change is significant enough to send a notification.
  ///   Notifies when crossing critical thresholds: < 20, < 50, or>= 80
  /// </summary>
  private static bool ShouldNotifyMoodChange(int oldMood, int newMood)
  {
    // Define critical thresholds
    const int criticalLow = 20;
    const int warningLow = 50;
    const int excellent = 80;

    // Check if crossed critical low threshold (< 20)
    if ((oldMood >= criticalLow && newMood < criticalLow) ||
        (oldMood < criticalLow && newMood >= criticalLow))
      return true;

    // Check if crossed warning threshold (< 50)
    if ((oldMood >= warningLow && newMood < warningLow) ||
        (oldMood < warningLow && newMood >= warningLow))
      return true;

    // Check if crossed excellent threshold (>= 80)
    if ((oldMood < excellent && newMood >= excellent) ||
        (oldMood >= excellent && newMood < excellent))
      return true;

    return false;
  }
}
