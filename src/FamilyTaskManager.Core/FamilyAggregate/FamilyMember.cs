using FamilyTaskManager.Core.SpotAggregate;
using FamilyTaskManager.Core.UserAggregate;

namespace FamilyTaskManager.Core.FamilyAggregate;

public class FamilyMember : EntityBase<FamilyMember, Guid>
{
  private readonly List<Spot> _responsibleSpots = [];

  private FamilyMember()
  {
  }

  public FamilyMember(Guid userId, Guid familyId, FamilyRole role)
  {
    Guard.Against.Default(userId);
    Guard.Against.Default(familyId);

    Id = Guid.NewGuid();
    UserId = userId;
    FamilyId = familyId;
    Role = role;
    Points = 0;
    JoinedAt = DateTime.UtcNow;
    IsActive = true;
  }

  public Guid UserId { get; private set; }
  public User User { get; private set; } = null!;
  public Guid FamilyId { get; private set; }
  public Family Family { get; private set; } = null!;
  public FamilyRole Role { get; private set; }
  public int Points { get; private set; }
  public DateTime JoinedAt { get; private set; }
  public bool IsActive { get; private set; }

  public IReadOnlyCollection<Spot> ResponsibleSpots => _responsibleSpots.AsReadOnly();

  public void AddPoints(int value)
  {
    Guard.Against.Negative(value);

    Points += value;
    if (Points < 0) Points = 0;
  }

  public void Deactivate() => IsActive = false;

  public void UpdateRole(FamilyRole role) => Role = role;
}
