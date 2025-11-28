using FamilyTaskManager.Core.TaskAggregate;

namespace FamilyTaskManager.UnitTests.Core.TaskAggregate;

public class TaskTemplateTests
{
  [Fact]
  public void Constructor_WithValidParameters_CreatesTaskTemplate()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var petId = Guid.NewGuid();
    var title = "Feed the pet";
    var points = 10;
    var schedule = "0 0 9 * * ?";
    var createdBy = Guid.NewGuid();

    // Act
    var template = new TaskTemplate(familyId, petId, title, points, schedule, TimeSpan.FromHours(12), createdBy);

    // Assert
    template.FamilyId.ShouldBe(familyId);
    template.PetId.ShouldBe(petId);
    template.Title.ShouldBe(title);
    template.Points.ShouldBe(points);
    template.Schedule.ShouldBe(schedule);
    template.CreatedBy.ShouldBe(createdBy);
    template.IsActive.ShouldBeTrue();
    template.CreatedAt.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-1), DateTime.UtcNow.AddSeconds(1));
  }

  [Fact]
  public void Constructor_WithWhitespace_TrimsTitleAndSchedule()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var petId = Guid.NewGuid();
    var title = "  Feed the pet  ";
    var points = 10;
    var schedule = "  0 0 9 * * ?  ";
    var createdBy = Guid.NewGuid();

    // Act
    var template = new TaskTemplate(familyId, petId, title, points, schedule, TimeSpan.FromHours(12), createdBy);

    // Assert
    template.Title.ShouldBe("Feed the pet");
    template.Schedule.ShouldBe("0 0 9 * * ?");
  }

  [Fact]
  public void Constructor_WithEmptyFamilyId_ThrowsException()
  {
    // Arrange
    var familyId = Guid.Empty;
    var petId = Guid.NewGuid();
    var title = "Feed the pet";
    var points = 10;
    var schedule = "0 0 9 * * ?";
    var createdBy = Guid.NewGuid();

    // Act & Assert
    Should.Throw<ArgumentException>(() =>
      new TaskTemplate(familyId, petId, title, points, schedule, TimeSpan.FromHours(12), createdBy));
  }

  [Fact]
  public void Constructor_WithEmptyPetId_ThrowsException()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var petId = Guid.Empty;
    var title = "Feed the pet";
    var points = 10;
    var schedule = "0 0 9 * * ?";
    var createdBy = Guid.NewGuid();

    // Act & Assert
    Should.Throw<ArgumentException>(() =>
      new TaskTemplate(familyId, petId, title, points, schedule, TimeSpan.FromHours(12), createdBy));
  }

  [Fact]
  public void Constructor_WithEmptyCreatedBy_ThrowsException()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var petId = Guid.NewGuid();
    var title = "Feed the pet";
    var points = 10;
    var schedule = "0 0 9 * * ?";
    var createdBy = Guid.Empty;

    // Act & Assert
    Should.Throw<ArgumentException>(() =>
      new TaskTemplate(familyId, petId, title, points, schedule, TimeSpan.FromHours(12), createdBy));
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void Constructor_WithInvalidTitle_ThrowsException(string? invalidTitle)
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var petId = Guid.NewGuid();
    var points = 10;
    var schedule = "0 0 9 * * ?";
    var createdBy = Guid.NewGuid();

    // Act & Assert
    Should.Throw<ArgumentException>(() =>
      new TaskTemplate(familyId, petId, invalidTitle!, points, schedule, TimeSpan.FromHours(12), createdBy));
  }

  [Theory]
  [InlineData(0)]
  [InlineData(-1)]
  [InlineData(101)]
  [InlineData(200)]
  public void Constructor_WithInvalidPoints_ThrowsException(int invalidPoints)
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var petId = Guid.NewGuid();
    var title = "Feed the pet";
    var schedule = "0 0 9 * * ?";
    var createdBy = Guid.NewGuid();

    // Act & Assert
    Should.Throw<ArgumentException>(() =>
      new TaskTemplate(familyId, petId, title, invalidPoints, schedule, TimeSpan.FromHours(12), createdBy));
  }

  [Theory]
  [InlineData(1)]
  [InlineData(50)]
  [InlineData(100)]
  public void Constructor_WithValidPoints_CreatesTemplate(int validPoints)
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var petId = Guid.NewGuid();
    var title = "Feed the pet";
    var schedule = "0 0 9 * * ?";
    var createdBy = Guid.NewGuid();

    // Act
    var template = new TaskTemplate(familyId, petId, title, validPoints, schedule, TimeSpan.FromHours(12), createdBy);

    // Assert
    template.Points.ShouldBe(validPoints);
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void Constructor_WithInvalidSchedule_ThrowsException(string? invalidSchedule)
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var petId = Guid.NewGuid();
    var title = "Feed the pet";
    var points = 10;
    var createdBy = Guid.NewGuid();

    // Act & Assert
    Should.Throw<ArgumentException>(() =>
      new TaskTemplate(familyId, petId, title, points, invalidSchedule!, TimeSpan.FromHours(12), createdBy));
  }

  [Fact]
  public void Update_WithValidParameters_UpdatesTemplate()
  {
    // Arrange
    var template = new TaskTemplate(
      Guid.NewGuid(), Guid.NewGuid(), "Feed the pet", 10, "0 0 9 * * ?", TimeSpan.FromHours(12), Guid.NewGuid());
    var newTitle = "Walk the dog";
    var newPoints = 20;
    var newSchedule = "0 0 18 * * ?";

    // Act
    template.Update(newTitle, newPoints, newSchedule, TimeSpan.FromHours(12));

    // Assert
    template.Title.ShouldBe(newTitle);
    template.Points.ShouldBe(newPoints);
    template.Schedule.ShouldBe(newSchedule);
  }

  [Fact]
  public void Update_WithWhitespace_TrimsTitleAndSchedule()
  {
    // Arrange
    var template = new TaskTemplate(
      Guid.NewGuid(), Guid.NewGuid(), "Feed the pet", 10, "0 0 9 * * ?", TimeSpan.FromHours(12), Guid.NewGuid());
    var newTitle = "  Walk the dog  ";
    var newPoints = 20;
    var newSchedule = "  0 0 18 * * ?  ";

    // Act
    template.Update(newTitle, newPoints, newSchedule, TimeSpan.FromHours(12));

    // Assert
    template.Title.ShouldBe("Walk the dog");
    template.Schedule.ShouldBe("0 0 18 * * ?");
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void Update_WithInvalidTitle_ThrowsException(string? invalidTitle)
  {
    // Arrange
    var template = new TaskTemplate(
      Guid.NewGuid(), Guid.NewGuid(), "Feed the pet", 10, "0 0 9 * * ?", TimeSpan.FromHours(12), Guid.NewGuid());

    // Act & Assert
    Should.Throw<ArgumentException>(() => template.Update(invalidTitle!, 20, "0 0 18 * * ?", TimeSpan.FromHours(12)));
  }

  [Theory]
  [InlineData(0)]
  [InlineData(-1)]
  [InlineData(101)]
  public void Update_WithInvalidPoints_ThrowsException(int invalidPoints)
  {
    // Arrange
    var template = new TaskTemplate(
      Guid.NewGuid(), Guid.NewGuid(), "Feed the pet", 10, "0 0 9 * * ?", TimeSpan.FromHours(12), Guid.NewGuid());

    // Act & Assert
    Should.Throw<ArgumentException>(() =>
      template.Update("Walk the dog", invalidPoints, "0 0 18 * * ?", TimeSpan.FromHours(12)));
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void Update_WithInvalidSchedule_ThrowsException(string? invalidSchedule)
  {
    // Arrange
    var template = new TaskTemplate(
      Guid.NewGuid(), Guid.NewGuid(), "Feed the pet", 10, "0 0 9 * * ?", TimeSpan.FromHours(12), Guid.NewGuid());

    // Act & Assert
    Should.Throw<ArgumentException>(() =>
      template.Update("Walk the dog", 20, invalidSchedule!, TimeSpan.FromHours(12)));
  }

  [Fact]
  public void Deactivate_SetsIsActiveToFalse()
  {
    // Arrange
    var template = new TaskTemplate(
      Guid.NewGuid(), Guid.NewGuid(), "Feed the pet", 10, "0 0 9 * * ?", TimeSpan.FromHours(12), Guid.NewGuid());
    template.IsActive.ShouldBeTrue();

    // Act
    template.Deactivate();

    // Assert
    template.IsActive.ShouldBeFalse();
  }

  [Fact]
  public void Deactivate_CalledMultipleTimes_RemainsInactive()
  {
    // Arrange
    var template = new TaskTemplate(
      Guid.NewGuid(), Guid.NewGuid(), "Feed the pet", 10, "0 0 9 * * ?", TimeSpan.FromHours(12), Guid.NewGuid());

    // Act
    template.Deactivate();
    template.Deactivate();

    // Assert
    template.IsActive.ShouldBeFalse();
  }

  [Fact]
  public void Deactivate_DoesNotAffectOtherProperties()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var petId = Guid.NewGuid();
    var title = "Feed the pet";
    var points = 10;
    var schedule = "0 0 9 * * ?";
    var createdBy = Guid.NewGuid();
    var template = new TaskTemplate(familyId, petId, title, points, schedule, TimeSpan.FromHours(12), createdBy);

    // Act
    template.Deactivate();

    // Assert
    template.FamilyId.ShouldBe(familyId);
    template.PetId.ShouldBe(petId);
    template.Title.ShouldBe(title);
    template.Points.ShouldBe(points);
    template.Schedule.ShouldBe(schedule);
    template.CreatedBy.ShouldBe(createdBy);
  }
}
