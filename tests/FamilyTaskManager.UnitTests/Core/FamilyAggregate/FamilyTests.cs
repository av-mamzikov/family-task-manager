using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.FamilyAggregate.Events;

namespace FamilyTaskManager.UnitTests.Core.FamilyAggregate;

public class FamilyTests
{
  [Fact]
  public void Constructor_WithValidParameters_CreatesFamily()
  {
    // Arrange
    var name = "Smith Family";
    var timezone = "UTC";
    var leaderboardEnabled = true;

    // Act
    var family = new Family(name, timezone, leaderboardEnabled);

    // Assert
    family.Name.ShouldBe(name);
    family.Timezone.ShouldBe(timezone);
    family.LeaderboardEnabled.ShouldBe(leaderboardEnabled);
    family.CreatedAt.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-1), DateTime.UtcNow.AddSeconds(1));
    family.Members.ShouldBeEmpty();
  }

  [Fact]
  public void Constructor_WithDefaultLeaderboard_EnablesLeaderboard()
  {
    // Arrange
    var name = "Smith Family";
    var timezone = "UTC";

    // Act
    var family = new Family(name, timezone);

    // Assert
    family.LeaderboardEnabled.ShouldBeTrue();
  }

  [Fact]
  public void Constructor_WithWhitespace_TrimsName()
  {
    // Arrange
    var name = "  Smith Family  ";
    var timezone = "UTC";

    // Act
    var family = new Family(name, timezone);

    // Assert
    family.Name.ShouldBe("Smith Family");
  }

  [Fact]
  public void Constructor_RaisesFamilyCreatedEvent()
  {
    // Arrange
    var name = "Smith Family";
    var timezone = "UTC";

    // Act
    var family = new Family(name, timezone);

    // Assert
    family.DomainEvents.ShouldContain(e => e is FamilyCreatedEvent);
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void Constructor_WithInvalidName_ThrowsException(string? invalidName)
  {
    // Arrange
    var timezone = "UTC";

    // Act & Assert
    Should.Throw<ArgumentException>(() => new Family(invalidName!, timezone));
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void Constructor_WithInvalidTimezone_ThrowsException(string? invalidTimezone)
  {
    // Arrange
    var name = "Smith Family";

    // Act & Assert
    Should.Throw<ArgumentException>(() => new Family(name, invalidTimezone!));
  }

  [Fact]
  public void AddMember_WithValidParameters_AddsMember()
  {
    // Arrange
    var family = new Family("Smith Family", "UTC");
    var userId = Guid.NewGuid();
    var role = FamilyRole.Admin;

    // Act
    var member = family.AddMember(userId, role);

    // Assert
    member.ShouldNotBeNull();
    member.UserId.ShouldBe(userId);
    member.FamilyId.ShouldBe(family.Id);
    member.Role.ShouldBe(role);
    family.Members.ShouldContain(member);
    family.Members.Count.ShouldBe(1);
  }

  [Fact]
  public void AddMember_RaisesMemberAddedEvent()
  {
    // Arrange
    var family = new Family("Smith Family", "UTC");
    var userId = Guid.NewGuid();
    var familyId = Guid.NewGuid();
    var role = FamilyRole.Admin;

    // Act
    family.AddMember(userId, role);

    // Assert
    family.DomainEvents.ShouldContain(e => e is MemberAddedEvent);
  }

  [Fact]
  public void AddMember_MultipleMembers_AddsAllMembers()
  {
    // Arrange
    var family = new Family("Smith Family", "UTC");
    var userId1 = Guid.NewGuid();
    var userId2 = Guid.NewGuid();
    var familyId = Guid.NewGuid();

    // Act
    var member1 = family.AddMember(userId1, FamilyRole.Admin);
    var member2 = family.AddMember(userId2, FamilyRole.Child);

    // Assert
    family.Members.Count.ShouldBe(2);
    family.Members.ShouldContain(member1);
    family.Members.ShouldContain(member2);
  }

  [Fact]
  public void UpdateSettings_WithValidParameters_UpdatesSettings()
  {
    // Arrange
    var family = new Family("Smith Family", "UTC");
    var newLeaderboardEnabled = false;
    var newTimezone = "Europe/Moscow";

    // Act
    family.UpdateSettings(newLeaderboardEnabled, newTimezone);

    // Assert
    family.LeaderboardEnabled.ShouldBe(newLeaderboardEnabled);
    family.Timezone.ShouldBe(newTimezone);
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void UpdateSettings_WithInvalidTimezone_ThrowsException(string? invalidTimezone)
  {
    // Arrange
    var family = new Family("Smith Family", "UTC");

    // Act & Assert
    Should.Throw<ArgumentException>(() => family.UpdateSettings(true, invalidTimezone!));
  }
}
