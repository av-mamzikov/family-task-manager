namespace FamilyTaskManager.Core.FamilyAggregate;

public class Family : EntityBase<Family, Guid>, IAggregateRoot
{
  public string Name { get; private set; } = null!;
  public DateTime CreatedAt { get; private set; }
  public string Timezone { get; private set; } = null!;
  public bool LeaderboardEnabled { get; private set; }

  private readonly List<FamilyMember> _members = [];
  public IReadOnlyCollection<FamilyMember> Members => _members.AsReadOnly();

  private Family() { }

  public Family(string name, string timezone, bool leaderboardEnabled = true)
  {
    Guard.Against.NullOrWhiteSpace(name);
    Guard.Against.NullOrWhiteSpace(timezone);

    Name = name.Trim();
    Timezone = timezone;
    LeaderboardEnabled = leaderboardEnabled;
    CreatedAt = DateTime.UtcNow;

    RegisterDomainEvent(new Events.FamilyCreatedEvent(this));
  }

  public FamilyMember AddMember(Guid userId, Guid familyId, FamilyRole role)
  {
    var member = new FamilyMember(userId, familyId, role);
    _members.Add(member);

    RegisterDomainEvent(new Events.MemberAddedEvent(this, member));

    return member;
  }

  public void UpdateSettings(bool leaderboardEnabled, string timezone)
  {
    Guard.Against.NullOrWhiteSpace(timezone);

    LeaderboardEnabled = leaderboardEnabled;
    Timezone = timezone;
  }
}
