namespace FamilyTaskManager.Core.FamilyAggregate;

public class FamilyMember : EntityBase<FamilyMember, Guid>
{
  private FamilyMember()
  {
  }

  public FamilyMember(Guid userId, Guid familyId, FamilyRole role)
  {
    Guard.Against.Default(userId);
    Guard.Against.Default(familyId);

    UserId = userId;
    FamilyId = familyId;
    Role = role;
    Points = 0;
    JoinedAt = DateTime.UtcNow;
    IsActive = true;
  }

  public Guid UserId { get; private set; }
  public Guid FamilyId { get; private set; }
  public FamilyRole Role { get; private set; }
  public int Points { get; private set; }
  public DateTime JoinedAt { get; private set; }
  public bool IsActive { get; private set; }

  public void AddPoints(int value)
  {
    Guard.Against.Negative(value);

    Points += value;
    if (Points < 0)
    {
      Points = 0;
    }
  }

  public void Deactivate() => IsActive = false;
}
