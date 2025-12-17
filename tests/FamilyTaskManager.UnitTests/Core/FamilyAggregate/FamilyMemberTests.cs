using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.UserAggregate;

namespace FamilyTaskManager.UnitTests.Core.FamilyAggregate;

public class FamilyMemberTests
{
  [Fact]
  public void Constructor_WithValidParameters_CreatesFamilyMember()
  {
    // Arrange
    var user = new User(0, "Name");
    var family = new Family("FamilyName", "UTC");
    var role = FamilyRole.Admin;

    // Act
    var member = new FamilyMember(user, family, role);

    // Assert
    member.UserId.ShouldBe(user.Id);
    member.FamilyId.ShouldBe(family.Id);
    member.Role.ShouldBe(role);
    member.Points.ShouldBe(0);
    member.IsActive.ShouldBeTrue();
    member.JoinedAt.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-1), DateTime.UtcNow.AddSeconds(1));
  }

  [Theory]
  [InlineData(FamilyRole.Admin)]
  [InlineData(FamilyRole.Adult)]
  [InlineData(FamilyRole.Child)]
  public void Constructor_WithDifferentRoles_CreatesWithCorrectRole(FamilyRole role)
  {
    // Arrange
    var user = new User(1, "Name");
    var family = new Family("family", "UTC");

    // Act
    var member = new FamilyMember(user, family, role);

    // Assert
    member.Role.ShouldBe(role);
  }

  [Fact]
  public void AddPoints_Multiple_AccumulatesPoints()
  {
    // Arrange
    var member = new FamilyMember(new(0, "user"), new("family", "UTC"), FamilyRole.Child);

    // Act
    member.AddPoints(10);
    member.AddPoints(5);
    member.AddPoints(15);

    // Assert
    member.Points.ShouldBe(30);
  }

  [Fact]
  public void Deactivate_SetsIsActiveToFalse()
  {
    // Arrange
    var member = new FamilyMember(new(0, "user"), new("family", "UTC"), FamilyRole.Child);
    member.IsActive.ShouldBeTrue();

    // Act
    member.Deactivate();

    // Assert
    member.IsActive.ShouldBeFalse();
  }

  [Fact]
  public void Deactivate_CalledMultipleTimes_RemainsInactive()
  {
    // Arrange
    var member = new FamilyMember(new(0, "user"), new("family", "UTC"), FamilyRole.Child);

    // Act
    member.Deactivate();
    member.Deactivate();

    // Assert
    member.IsActive.ShouldBeFalse();
  }

  [Fact]
  public void Deactivate_DoesNotAffectPoints()
  {
    // Arrange
    var member = new FamilyMember(new(0, "user"), new("family", "UTC"), FamilyRole.Child);
    member.AddPoints(50);

    // Act
    member.Deactivate();

    // Assert
    member.Points.ShouldBe(50);
    member.IsActive.ShouldBeFalse();
  }
}
