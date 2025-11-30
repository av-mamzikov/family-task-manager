using FamilyTaskManager.Core.PetAggregate;

namespace FamilyTaskManager.UnitTests.Core.PetAggregate;

public class PetTests
{
  [Fact]
  public void Constructor_WithValidParameters_CreatesPet()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var type = PetType.Cat;
    var name = "Whiskers";

    // Act
    var pet = new Pet(familyId, type, name);

    // Assert
    pet.FamilyId.ShouldBe(familyId);
    pet.Type.ShouldBe(type);
    pet.Name.ShouldBe(name);
    pet.MoodScore.ShouldBe(100);
    pet.CreatedAt.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-1), DateTime.UtcNow.AddSeconds(1));
  }

  [Fact]
  public void Constructor_WithWhitespace_TrimsName()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var type = PetType.Dog;
    var name = "  Buddy  ";

    // Act
    var pet = new Pet(familyId, type, name);

    // Assert
    pet.Name.ShouldBe("Buddy");
  }

  [Theory]
  [InlineData(PetType.Cat)]
  [InlineData(PetType.Dog)]
  [InlineData(PetType.Hamster)]
  public void Constructor_WithDifferentTypes_CreatesWithCorrectType(PetType type)
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var name = "TestPet";

    // Act
    var pet = new Pet(familyId, type, name);

    // Assert
    pet.Type.ShouldBe(type);
  }

  [Fact]
  public void Constructor_WithEmptyFamilyId_ThrowsException()
  {
    // Arrange
    var familyId = Guid.Empty;
    var type = PetType.Cat;
    var name = "Whiskers";

    // Act & Assert
    Should.Throw<ArgumentException>(() => new Pet(familyId, type, name));
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void Constructor_WithInvalidName_ThrowsException(string? invalidName)
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var type = PetType.Cat;

    // Act & Assert
    Should.Throw<ArgumentException>(() => new Pet(familyId, type, invalidName!));
  }

  [Fact]
  public void UpdateName_WithValidName_UpdatesName()
  {
    // Arrange
    var pet = new Pet(Guid.NewGuid(), PetType.Cat, "Whiskers");
    var newName = "Fluffy";

    // Act
    pet.UpdateName(newName);

    // Assert
    pet.Name.ShouldBe(newName);
  }

  [Fact]
  public void UpdateName_WithWhitespace_TrimsName()
  {
    // Arrange
    var pet = new Pet(Guid.NewGuid(), PetType.Cat, "Whiskers");
    var newName = "  Fluffy  ";

    // Act
    pet.UpdateName(newName);

    // Assert
    pet.Name.ShouldBe("Fluffy");
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void UpdateName_WithInvalidName_ThrowsException(string? invalidName)
  {
    // Arrange
    var pet = new Pet(Guid.NewGuid(), PetType.Cat, "Whiskers");

    // Act & Assert
    Should.Throw<ArgumentException>(() => pet.UpdateName(invalidName!));
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
    var pet = new Pet(Guid.NewGuid(), PetType.Cat, "Whiskers");

    // Act
    pet.UpdateMoodScore(score);

    // Assert
    pet.MoodScore.ShouldBe(score);
  }

  [Fact]
  public void UpdateMoodScore_WithScoreAbove100_ClampsTo100()
  {
    // Arrange
    var pet = new Pet(Guid.NewGuid(), PetType.Cat, "Whiskers");

    // Act
    pet.UpdateMoodScore(150);

    // Assert
    pet.MoodScore.ShouldBe(100);
  }

  [Fact]
  public void UpdateMoodScore_WithScoreBelow0_ClampsTo0()
  {
    // Arrange
    var pet = new Pet(Guid.NewGuid(), PetType.Cat, "Whiskers");

    // Act
    pet.UpdateMoodScore(-50);

    // Assert
    pet.MoodScore.ShouldBe(0);
  }

  [Fact]
  public void UpdateMoodScore_Multiple_UpdatesToLatestValue()
  {
    // Arrange
    var pet = new Pet(Guid.NewGuid(), PetType.Cat, "Whiskers");

    // Act
    pet.UpdateMoodScore(80);
    pet.UpdateMoodScore(30);
    pet.UpdateMoodScore(60);

    // Assert
    pet.MoodScore.ShouldBe(60);
  }

  [Fact]
  public void UpdateMoodScore_WithBoundaryValues_ClampsCorrectly()
  {
    // Arrange
    var pet = new Pet(Guid.NewGuid(), PetType.Cat, "Whiskers");

    // Act & Assert
    pet.UpdateMoodScore(-1);
    pet.MoodScore.ShouldBe(0);

    pet.UpdateMoodScore(101);
    pet.MoodScore.ShouldBe(100);

    pet.UpdateMoodScore(0);
    pet.MoodScore.ShouldBe(0);

    pet.UpdateMoodScore(100);
    pet.MoodScore.ShouldBe(100);
  }
}
