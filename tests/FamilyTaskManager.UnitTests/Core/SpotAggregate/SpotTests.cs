using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.SpotAggregate;
using FamilyTaskManager.UnitTests.Helpers;

namespace FamilyTaskManager.UnitTests.Core.SpotAggregate;

public class SpotTests
{
  [Fact]
  public void Constructor_WithValidParameters_CreatesSpot()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var type = SpotType.Cat;
    var name = "Whiskers";

    // Act
    var spot = new Spot(familyId, type, name);

    // Assert
    spot.FamilyId.ShouldBe(familyId);
    spot.Type.ShouldBe(type);
    spot.Name.ShouldBe(name);
    spot.MoodScore.ShouldBe(100);
    spot.CreatedAt.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-1), DateTime.UtcNow.AddSeconds(1));
  }

  [Fact]
  public void Constructor_WithWhitespace_TrimsName()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var type = SpotType.Dog;
    var name = "  Buddy  ";

    // Act
    var spot = new Spot(familyId, type, name);

    // Assert
    spot.Name.ShouldBe("Buddy");
  }

  [Theory]
  [InlineData(SpotType.Cat)]
  [InlineData(SpotType.Dog)]
  [InlineData(SpotType.Hamster)]
  public void Constructor_WithDifferentTypes_CreatesWithCorrectType(SpotType type)
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var name = "TestSpot";

    // Act
    var spot = new Spot(familyId, type, name);

    // Assert
    spot.Type.ShouldBe(type);
  }

  [Fact]
  public void Constructor_WithEmptyFamilyId_ThrowsException()
  {
    // Arrange
    var familyId = Guid.Empty;
    var type = SpotType.Cat;
    var name = "Whiskers";

    // Act & Assert
    Should.Throw<ArgumentException>(() => new Spot(familyId, type, name));
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void Constructor_WithInvalidName_ThrowsException(string? invalidName)
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var type = SpotType.Cat;

    // Act & Assert
    Should.Throw<ArgumentException>(() => new Spot(familyId, type, invalidName!));
  }

  [Fact]
  public void UpdateName_WithValidName_UpdatesName()
  {
    // Arrange
    var spot = new Spot(Guid.NewGuid(), SpotType.Cat, "Whiskers");
    var newName = "Fluffy";

    // Act
    spot.UpdateName(newName);

    // Assert
    spot.Name.ShouldBe(newName);
  }

  [Fact]
  public void UpdateName_WithWhitespace_TrimsName()
  {
    // Arrange
    var spot = new Spot(Guid.NewGuid(), SpotType.Cat, "Whiskers");
    var newName = "  Fluffy  ";

    // Act
    spot.UpdateName(newName);

    // Assert
    spot.Name.ShouldBe("Fluffy");
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void UpdateName_WithInvalidName_ThrowsException(string? invalidName)
  {
    // Arrange
    var spot = new Spot(Guid.NewGuid(), SpotType.Cat, "Whiskers");

    // Act & Assert
    Should.Throw<ArgumentException>(() => spot.UpdateName(invalidName!));
  }

  [Theory]
  [InlineData(0)]
  [InlineData(25)]
  [InlineData(50)]
  [InlineData(75)]
  [InlineData(100)]
  public void UpdateMoodScore_WithValidScore_UpdatesMoodScore(int score)
  {
    // Arrange
    var spot = new Spot(Guid.NewGuid(), SpotType.Cat, "Whiskers");

    // Act
    spot.UpdateMoodScore(score);

    // Assert
    spot.MoodScore.ShouldBe(score);
  }

  [Fact]
  public void UpdateMoodScore_WithScoreAbove100_ClampsTo100()
  {
    // Arrange
    var spot = new Spot(Guid.NewGuid(), SpotType.Cat, "Whiskers");

    // Act
    spot.UpdateMoodScore(150);

    // Assert
    spot.MoodScore.ShouldBe(100);
  }

  [Fact]
  public void UpdateMoodScore_WithScoreBelow0_ClampsTo0()
  {
    // Arrange
    var spot = new Spot(Guid.NewGuid(), SpotType.Cat, "Whiskers");

    // Act
    spot.UpdateMoodScore(-50);

    // Assert
    spot.MoodScore.ShouldBe(0);
  }

  [Fact]
  public void UpdateMoodScore_Multiple_UpdatesToLatestValue()
  {
    // Arrange
    var spot = new Spot(Guid.NewGuid(), SpotType.Cat, "Whiskers");

    // Act
    spot.UpdateMoodScore(80);
    spot.UpdateMoodScore(30);
    spot.UpdateMoodScore(60);

    // Assert
    spot.MoodScore.ShouldBe(60);
  }

  [Fact]
  public void UpdateMoodScore_WithBoundaryValues_ClampsCorrectly()
  {
    // Arrange
    var spot = new Spot(Guid.NewGuid(), SpotType.Cat, "Whiskers");

    // Act & Assert
    spot.UpdateMoodScore(-1);
    spot.MoodScore.ShouldBe(0);

    spot.UpdateMoodScore(101);
    spot.MoodScore.ShouldBe(100);

    spot.UpdateMoodScore(0);
    spot.MoodScore.ShouldBe(0);

    spot.UpdateMoodScore(100);
    spot.MoodScore.ShouldBe(100);
  }

  [Fact]
  public void AssignResponsible_WithValidMember_AddsMemberToResponsibleCollection()
  {
    var spot = TestHelpers.CreateSpotWithFamily();
    var member = TestHelpers.CreateMemberWithUser();

    // Ensure member belongs to the same family as spot
    typeof(FamilyMember).GetProperty("FamilyId")!.SetValue(member, spot.FamilyId);

    spot.AssignResponsible(member);

    spot.ResponsibleMembers.ShouldContain(m => m.Id == member.Id);
  }

  [Fact]
  public void AssignResponsible_SameMemberTwice_DoesNotCreateDuplicates()
  {
    var spot = TestHelpers.CreateSpotWithFamily();
    var member = TestHelpers.CreateMemberWithUser();

    typeof(FamilyMember).GetProperty("FamilyId")!.SetValue(member, spot.FamilyId);

    spot.AssignResponsible(member);
    spot.AssignResponsible(member);

    spot.ResponsibleMembers.Count(m => m.Id == member.Id).ShouldBe(1);
  }

  [Fact]
  public void AssignResponsible_MemberFromAnotherFamily_ThrowsException()
  {
    var spot = TestHelpers.CreateSpotWithFamily();
    var member = TestHelpers.CreateMemberWithUser();

    typeof(FamilyMember).GetProperty("FamilyId")!.SetValue(member, Guid.NewGuid());

    Should.Throw<ArgumentException>(() => spot.AssignResponsible(member));
  }

  [Fact]
  public void AssignResponsible_InactiveMember_ThrowsException()
  {
    var spot = TestHelpers.CreateSpotWithFamily();
    var member = TestHelpers.CreateMemberWithUser();

    typeof(FamilyMember).GetProperty("FamilyId")!.SetValue(member, spot.FamilyId);
    typeof(FamilyMember).GetProperty("IsActive")!.SetValue(member, false);

    Should.Throw<InvalidOperationException>(() => spot.AssignResponsible(member));
  }

  [Fact]
  public void RemoveResponsible_WhenMemberAssigned_RemovesFromCollection()
  {
    var spot = TestHelpers.CreateSpotWithFamily();
    var member = TestHelpers.CreateMemberWithUser();

    typeof(FamilyMember).GetProperty("FamilyId")!.SetValue(member, spot.FamilyId);

    spot.AssignResponsible(member);

    spot.RemoveResponsible(member);

    spot.ResponsibleMembers.ShouldNotContain(m => m.Id == member.Id);
  }

  [Fact]
  public void RemoveResponsible_WhenMemberNotAssigned_DoesNothing()
  {
    var spot = TestHelpers.CreateSpotWithFamily();
    var member = TestHelpers.CreateMemberWithUser();

    var initialCount = spot.ResponsibleMembers.Count;

    spot.RemoveResponsible(member);

    spot.ResponsibleMembers.Count.ShouldBe(initialCount);
  }
}
