namespace FamilyTaskManager.Core.PetAggregate;

public enum PetType
{
  Cat = 0,
  Dog = 1,
  Hamster = 2
}

public class Pet : EntityBase<Pet, Guid>, IAggregateRoot
{
  public Guid FamilyId { get; private set; }
  public PetType Type { get; private set; }
  public string Name { get; private set; } = null!;
  public int MoodScore { get; private set; }
  public DateTime CreatedAt { get; private set; }

  private Pet() { }

  public Pet(Guid familyId, PetType type, string name)
  {
    Guard.Against.Default(familyId);
    Guard.Against.NullOrWhiteSpace(name);

    FamilyId = familyId;
    Type = type;
    Name = name.Trim();
    MoodScore = 50;
    CreatedAt = DateTime.UtcNow;
  }

  public void UpdateName(string name)
  {
    Guard.Against.NullOrWhiteSpace(name);
    Name = name.Trim();
  }

  public void UpdateMoodScore(int moodScore)
  {
    MoodScore = Math.Clamp(moodScore, 0, 100);
  }
}
