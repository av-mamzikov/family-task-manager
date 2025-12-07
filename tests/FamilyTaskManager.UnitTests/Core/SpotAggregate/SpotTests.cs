using FamilyTaskManager.Core.SpotAggregate;

namespace FamilyTaskManager.UnitTests.Core.SpotAggregate;

public class SpotBowsingTests
{
  [Fact]
  public void Constructor_WithValidParameters_CreatesSpot()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var type = SpotType.Cat;
    var name = "Whiskers";

    // Act
    var Spot = new SpotBowsing(familyId, type, name);

    // Assert
    Spot.FamilyId.ShouldBe(familyId);
    Spot.Type.ShouldBe(type);
    Spot.Name.ShouldBe(name);
    Spot.MoodScore.ShouldBe(100);
    Spot.CreatedAt.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-1), DateTime.UtcNow.AddSeconds(1));
  }

  [Fact]
  public void Constructor_WithWhitespace_TrimsName()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var type = SpotType.Dog;
    var name = "  Buddy  ";

    // Act
    var Spot = new SpotBowsing(familyId, type, name);

    // Assert
    Spot.Name.ShouldBe("Buddy");
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
    var Spot = new SpotBowsing(familyId, type, name);

    // Assert
    Spot.Type.ShouldBe(type);
  }

  [Fact]
  public void Constructor_WithEmptyFamilyId_ThrowsException()
  {
    // Arrange
    var familyId = Guid.Empty;
    var type = SpotType.Cat;
    var name = "Whiskers";

    // Act & Assert
    Should.Throw<ArgumentException>(() => new SpotBowsing(familyId, type, name));
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
    Should.Throw<ArgumentException>(() => new SpotBowsing(familyId, type, invalidName!));
  }

  [Fact]
  public void UpdateName_WithValidName_UpdatesName()
  {
    // Arrange
    var Spot = new SpotBowsing(Guid.NewGuid(), SpotType.Cat, "Whiskers");
    var newName = "Fluffy";

    // Act
    Spot.UpdateName(newName);

    // Assert
    Spot.Name.ShouldBe(newName);
  }

  [Fact]
  public void UpdateName_WithWhitespace_TrimsName()
  {
    // Arrange
    var Spot = new SpotBowsing(Guid.NewGuid(), SpotType.Cat, "Whiskers");
    var newName = "  Fluffy  ";

    // Act
    Spot.UpdateName(newName);

    // Assert
    Spot.Name.ShouldBe("Fluffy");
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void UpdateName_WithInvalidName_ThrowsException(string? invalidName)
  {
    // Arrange
    var Spot = new SpotBowsing(Guid.NewGuid(), SpotType.Cat, "Whiskers");

    // Act & Assert
    Should.Throw<ArgumentException>(() => Spot.UpdateName(invalidName!));
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
    var Spot = new SpotBowsing(Guid.NewGuid(), SpotType.Cat, "Whiskers");

    // Act
    Spot.UpdateMoodScore(score);

    // Assert
    Spot.MoodScore.ShouldBe(score);
  }

  [Fact]
  public void UpdateMoodScore_WithScoreAbove100_ClampsTo100()
  {
    // Arrange
    var Spot = new SpotBowsing(Guid.NewGuid(), SpotType.Cat, "Whiskers");

    // Act
    Spot.UpdateMoodScore(150);

    // Assert
    Spot.MoodScore.ShouldBe(100);
  }

  [Fact]
  public void UpdateMoodScore_WithScoreBelow0_ClampsTo0()
  {
    // Arrange
    var Spot = new SpotBowsing(Guid.NewGuid(), SpotType.Cat, "Whiskers");

    // Act
    Spot.UpdateMoodScore(-50);

    // Assert
    Spot.MoodScore.ShouldBe(0);
  }

  [Fact]
  public void UpdateMoodScore_Multiple_UpdatesToLatestValue()
  {
    // Arrange
    var Spot = new SpotBowsing(Guid.NewGuid(), SpotType.Cat, "Whiskers");

    // Act
    Spot.UpdateMoodScore(80);
    Spot.UpdateMoodScore(30);
    Spot.UpdateMoodScore(60);

    // Assert
    Spot.MoodScore.ShouldBe(60);
  }

  [Fact]
  public void UpdateMoodScore_WithBoundaryValues_ClampsCorrectly()
  {
    // Arrange
    var Spot = new SpotBowsing(Guid.NewGuid(), SpotType.Cat, "Whiskers");

    // Act & Assert
    Spot.UpdateMoodScore(-1);
    Spot.MoodScore.ShouldBe(0);

    Spot.UpdateMoodScore(101);
    Spot.MoodScore.ShouldBe(100);

    Spot.UpdateMoodScore(0);
    Spot.MoodScore.ShouldBe(0);

    Spot.UpdateMoodScore(100);
    Spot.MoodScore.ShouldBe(100);
  }
}
