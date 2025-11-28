using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.PetAggregate.Events;

namespace FamilyTaskManager.Core.PetAggregate;

public enum PetType
{
  Cat = 0,
  Dog = 1,
  Hamster = 2
}

public class Pet : EntityBase<Pet, Guid>, IAggregateRoot
{
  private Pet()
  {
  }

  public Pet(Guid familyId, PetType type, string name)
  {
    Guard.Against.Default(familyId);
    Guard.Against.NullOrWhiteSpace(name);

    Id = Guid.NewGuid();
    FamilyId = familyId;
    Type = type;
    Name = name.Trim();
    MoodScore = 100;
    CreatedAt = DateTime.UtcNow;
  }

  public Guid FamilyId { get; private set; }
  public PetType Type { get; private set; }
  public string Name { get; private set; } = null!;

  // Navigation property
  public Family Family { get; private set; } = null!;

  /// <summary>
  ///   Денормализованное поле, которое рассчитывается периодически в зависимости от наличия невыполненных задач
  /// </summary>
  public int MoodScore { get; private set; }

  public DateTime CreatedAt { get; private set; }

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
    {
      RegisterDomainEvent(new PetMoodChangedEvent(this, oldMood, newMood));
    }
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
    {
      return true;
    }

    // Check if crossed warning threshold (< 50)
    if ((oldMood >= warningLow && newMood < warningLow) ||
        (oldMood < warningLow && newMood >= warningLow))
    {
      return true;
    }

    // Check if crossed excellent threshold (>= 80)
    if ((oldMood < excellent && newMood >= excellent) ||
        (oldMood >= excellent && newMood < excellent))
    {
      return true;
    }

    return false;
  }
}
