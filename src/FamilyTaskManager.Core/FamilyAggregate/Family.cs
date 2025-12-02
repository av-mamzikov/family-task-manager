using FamilyTaskManager.Core.FamilyAggregate.Events;
using FamilyTaskManager.Core.UserAggregate;

namespace FamilyTaskManager.Core.FamilyAggregate;

public class Family : EntityBase<Family, Guid>, IAggregateRoot
{
  private readonly List<FamilyMember> _members = [];

  private Family()
  {
  }

  public Family(string name, string timezone, bool leaderboardEnabled = true)
  {
    Guard.Against.NullOrWhiteSpace(name);
    Guard.Against.NullOrWhiteSpace(timezone);

    Id = Guid.NewGuid(); // Generate ID immediately
    Name = name.Trim();
    Timezone = timezone;
    LeaderboardEnabled = leaderboardEnabled;
    CreatedAt = DateTime.UtcNow;

    RegisterDomainEvent(new FamilyCreatedEvent(this));
  }

  public string Name { get; private set; } = null!;
  public DateTime CreatedAt { get; private set; }
  public string Timezone { get; private set; } = null!;
  public bool LeaderboardEnabled { get; private set; }
  public IReadOnlyCollection<FamilyMember> Members => _members.AsReadOnly();

  public FamilyMember AddMember(User user, FamilyRole role)
  {
    Guard.Against.Null(user);

    var member = new FamilyMember(user.Id, Id, role);
    _members.Add(member);

    RegisterDomainEvent(new MemberAddedEvent
    {
      FamilyId = Id,
      MemberId = member.Id,
      UserId = user.Id,
      UserName = user.Name
    });

    return member;
  }

  public void UpdateSettings(bool leaderboardEnabled, string timezone)
  {
    Guard.Against.NullOrWhiteSpace(timezone);

    LeaderboardEnabled = leaderboardEnabled;
    Timezone = timezone;
  }
}
