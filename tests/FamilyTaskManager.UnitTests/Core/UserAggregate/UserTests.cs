using FamilyTaskManager.Core.UserAggregate;

namespace FamilyTaskManager.UnitTests.Core.UserAggregate;

public class UserTests
{
  [Fact]
  public void Constructor_WithValidParameters_CreatesUser()
  {
    // Arrange
    var telegramId = 123456789L;
    var name = "John Doe";

    // Act
    var user = new User(telegramId, name);

    // Assert
    user.TelegramId.ShouldBe(telegramId);
    user.Name.ShouldBe(name);
    user.CreatedAt.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-1), DateTime.UtcNow.AddSeconds(1));
  }

  [Fact]
  public void Constructor_WithWhitespace_TrimsName()
  {
    // Arrange
    var telegramId = 123456789L;
    var name = "  John Doe  ";

    // Act
    var user = new User(telegramId, name);

    // Assert
    user.Name.ShouldBe("John Doe");
  }

  [Theory]
  [InlineData(0)]
  [InlineData(-1)]
  [InlineData(-100)]
  public void Constructor_WithInvalidTelegramId_ThrowsException(long invalidTelegramId)
  {
    // Arrange
    var name = "John Doe";

    // Act & Assert
    Should.Throw<ArgumentException>(() => new User(invalidTelegramId, name));
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void Constructor_WithInvalidName_ThrowsException(string? invalidName)
  {
    // Arrange
    var telegramId = 123456789L;

    // Act & Assert
    Should.Throw<ArgumentException>(() => new User(telegramId, invalidName!));
  }

  [Fact]
  public void UpdateName_WithValidName_UpdatesName()
  {
    // Arrange
    var user = new User(123456789L, "John Doe");
    var newName = "Jane Smith";

    // Act
    user.UpdateName(newName);

    // Assert
    user.Name.ShouldBe(newName);
  }

  [Fact]
  public void UpdateName_WithWhitespace_TrimsName()
  {
    // Arrange
    var user = new User(123456789L, "John Doe");
    var newName = "  Jane Smith  ";

    // Act
    user.UpdateName(newName);

    // Assert
    user.Name.ShouldBe("Jane Smith");
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void UpdateName_WithInvalidName_ThrowsException(string? invalidName)
  {
    // Arrange
    var user = new User(123456789L, "John Doe");

    // Act & Assert
    Should.Throw<ArgumentException>(() => user.UpdateName(invalidName!));
  }
}
