using System.Reflection;
using FamilyTaskManager.Core.FamilyAggregate;

namespace FamilyTaskManager.UnitTests.Core.FamilyAggregate;

public class InvitationTests
{
  [Fact]
  public void Constructor_ValidParameters_CreatesInvitation()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var createdBy = Guid.NewGuid();
    var role = FamilyRole.Adult;

    // Act
    var invitation = new Invitation(familyId, role, createdBy);

    // Assert
    invitation.FamilyId.ShouldBe(familyId);
    invitation.Role.ShouldBe(role);
    invitation.CreatedBy.ShouldBe(createdBy);
    invitation.IsActive.ShouldBeTrue();
    invitation.Code.ShouldNotBeNullOrEmpty();
    invitation.Code.Length.ShouldBe(8);
    invitation.CreatedAt.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-5), DateTime.UtcNow);
    invitation.ExpiresAt.ShouldNotBeNull();
    invitation.ExpiresAt.Value.ShouldBeInRange(DateTime.UtcNow.AddDays(6), DateTime.UtcNow.AddDays(8));
  }

  [Fact]
  public void Constructor_NoExpiration_CreatesInvitationWithoutExpiry()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var createdBy = Guid.NewGuid();

    // Act
    var invitation = new Invitation(familyId, FamilyRole.Admin, createdBy, 0);

    // Assert
    invitation.ExpiresAt.ShouldBeNull();
  }

  [Fact]
  public void Constructor_GeneratesUniqueCode()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var createdBy = Guid.NewGuid();

    // Act
    var invitation1 = new Invitation(familyId, FamilyRole.Adult, createdBy);
    var invitation2 = new Invitation(familyId, FamilyRole.Adult, createdBy);

    // Assert
    invitation1.Code.ShouldNotBe(invitation2.Code);
  }

  [Fact]
  public void Deactivate_SetsIsActiveToFalse()
  {
    // Arrange
    var invitation = new Invitation(Guid.NewGuid(), FamilyRole.Adult, Guid.NewGuid());

    // Act
    invitation.Deactivate();

    // Assert
    invitation.IsActive.ShouldBeFalse();
  }

  [Fact]
  public void IsExpired_NotExpired_ReturnsFalse()
  {
    // Arrange
    var invitation = new Invitation(Guid.NewGuid(), FamilyRole.Adult, Guid.NewGuid());

    // Act
    var result = invitation.IsExpired();

    // Assert
    result.ShouldBeFalse();
  }

  [Fact]
  public void IsExpired_Expired_ReturnsTrue()
  {
    // Arrange - Create invitation with very short expiration
    var invitation = new Invitation(Guid.NewGuid(), FamilyRole.Adult, Guid.NewGuid(), 0);

    // Manually set ExpiresAt to a past date using the private setter via reflection on the backing field
    var expiresAtField = typeof(Invitation).GetField("<ExpiresAt>k__BackingField",
      BindingFlags.NonPublic | BindingFlags.Instance);
    expiresAtField!.SetValue(invitation, DateTime.UtcNow.AddDays(-1));

    // Act
    var result = invitation.IsExpired();

    // Assert
    result.ShouldBeTrue();
  }

  [Fact]
  public void IsExpired_NoExpiration_ReturnsFalse()
  {
    // Arrange
    var invitation = new Invitation(Guid.NewGuid(), FamilyRole.Adult, Guid.NewGuid(), 0);

    // Act
    var result = invitation.IsExpired();

    // Assert
    result.ShouldBeFalse();
  }

  [Fact]
  public void IsValid_ActiveAndNotExpired_ReturnsTrue()
  {
    // Arrange
    var invitation = new Invitation(Guid.NewGuid(), FamilyRole.Adult, Guid.NewGuid());

    // Act
    var result = invitation.IsValid();

    // Assert
    result.ShouldBeTrue();
  }

  [Fact]
  public void IsValid_Inactive_ReturnsFalse()
  {
    // Arrange
    var invitation = new Invitation(Guid.NewGuid(), FamilyRole.Adult, Guid.NewGuid());
    invitation.Deactivate();

    // Act
    var result = invitation.IsValid();

    // Assert
    result.ShouldBeFalse();
  }

  [Fact]
  public void IsValid_Expired_ReturnsFalse()
  {
    // Arrange - Create invitation and set expired date via backing field
    var invitation = new Invitation(Guid.NewGuid(), FamilyRole.Adult, Guid.NewGuid(), 0);

    // Manually set ExpiresAt to a past date using the private setter via reflection on the backing field
    var expiresAtField = typeof(Invitation).GetField("<ExpiresAt>k__BackingField",
      BindingFlags.NonPublic | BindingFlags.Instance);
    expiresAtField!.SetValue(invitation, DateTime.UtcNow.AddDays(-1));

    // Act
    var result = invitation.IsValid();

    // Assert
    result.ShouldBeFalse();
  }

  [Fact]
  public void Code_OnlyContainsAllowedCharacters()
  {
    // Arrange & Act
    var invitation = new Invitation(Guid.NewGuid(), FamilyRole.Adult, Guid.NewGuid());
    var allowedChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    // Assert
    foreach (var c in invitation.Code)
    {
      allowedChars.ShouldContain(c.ToString());
    }
  }
}
