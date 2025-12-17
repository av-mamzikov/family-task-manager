using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.SpotAggregate;
using FamilyTaskManager.Core.TaskAggregate;
using FamilyTaskManager.Core.TaskAggregate.Events;
using FamilyTaskManager.Core.UserAggregate;
using TaskStatus = FamilyTaskManager.Core.TaskAggregate.TaskStatus;

namespace FamilyTaskManager.UnitTests.Core.TaskAggregate;

public class TaskInstanceTests
{
  private static Spot CreateSpotWithFamily(string spotName = "Test Spot", string timezone = "UTC")
  {
    var family = new Family("Test Family", timezone);
    var spot = new Spot(family.Id, SpotType.Cat, spotName);
    // Manually set navigation property for tests
    typeof(Spot).GetProperty("Family")!.SetValue(spot, family);
    return spot;
  }

  [Fact]
  public void Constructor_WithValidParameters_CreatesTaskInstance()
  {
    // Arrange
    var spot = CreateSpotWithFamily();
    var title = "Feed the Spot";
    var points = 2;
    var dueAt = DateTime.UtcNow.AddHours(2);

    // Act
    var task = new TaskInstance(spot, title, new(points), dueAt);

    // Assert
    task.FamilyId.ShouldBe(spot.FamilyId);
    task.SpotId.ShouldBe(spot.Id);
    task.Title.ShouldBe(title);
    task.Points.Value.ShouldBe(points);
    task.DueAt.ShouldBe(dueAt);
    task.Status.ShouldBe(TaskStatus.Active);
    task.TemplateId.ShouldBeNull();
    task.AssignedToMemberId.ShouldBeNull();
    task.CompletedByMember.ShouldBeNull();
    task.CompletedAt.ShouldBeNull();
    task.CreatedAt.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-1), DateTime.UtcNow.AddSeconds(1));
  }

  [Fact]
  public void Constructor_WithTemplateId_CreatesTaskInstanceWithTemplate()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var spotId = Guid.NewGuid();
    var title = "Feed the Spot";
    var points = 2;
    var dueAt = DateTime.UtcNow.AddHours(2);
    var templateId = Guid.NewGuid();

    // Act
    var spot = CreateSpotWithFamily();
    var task = new TaskInstance(spot, title, new(points), dueAt, templateId);

    // Assert
    task.TemplateId.ShouldBe(templateId);
    task.AssignedToMemberId.ShouldBeNull();
  }

  [Fact]
  public void Constructor_WithAssignedToMemberId_SetsAssignedToMemberId()
  {
    // Arrange
    var title = "Feed the Spot";
    var points = 2;
    var dueAt = DateTime.UtcNow.AddHours(2);
    var templateId = Guid.NewGuid();
    var assignedToMemberId = Guid.NewGuid();

    // Act
    var spot = CreateSpotWithFamily();
    var task = new TaskInstance(spot, title, new(points), dueAt, templateId, assignedToMemberId);

    // Assert
    task.AssignedToMemberId.ShouldBe(assignedToMemberId);
  }

  [Fact]
  public void Constructor_WithWhitespace_TrimsTitle()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var spotId = Guid.NewGuid();
    var title = "  Feed the Spot  ";
    var points = 2;
    var dueAt = DateTime.UtcNow.AddHours(2);

    // Act
    var spot = CreateSpotWithFamily();
    var task = new TaskInstance(spot, title, new(points), dueAt);

    // Assert
    task.Title.ShouldBe("Feed the Spot");
  }

  [Fact]
  public void Constructor_RaisesTaskCreatedEvent()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var spotId = Guid.NewGuid();
    var title = "Feed the Spot";
    var points = 2;
    var dueAt = DateTime.UtcNow.AddHours(2);

    // Act
    var spot = CreateSpotWithFamily();
    var task = new TaskInstance(spot, title, new(points), dueAt);

    // Assert
    task.DomainEvents.ShouldContain(e => e is TaskCreatedEvent);
  }

  [Fact]
  public void Constructor_WithEmptyFamilyId_ThrowsException()
  {
    // Arrange
    var familyId = Guid.Empty;
    var spotId = Guid.NewGuid();
    var title = "Feed the Spot";
    var points = 10;
    var dueAt = DateTime.UtcNow.AddHours(2);

    // Act & Assert
    Should.Throw<ArgumentException>(() =>
      new TaskInstance(CreateSpotWithFamily(), title, new(points), dueAt));
  }

  [Fact]
  public void Constructor_WithEmptySpotId_ThrowsException()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var spotId = Guid.Empty;
    var title = "Feed the Spot";
    var points = 10;
    var dueAt = DateTime.UtcNow.AddHours(2);

    // Act & Assert
    Should.Throw<ArgumentException>(() =>
      new TaskInstance(CreateSpotWithFamily(), title, new(points), dueAt));
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void Constructor_WithInvalidTitle_ThrowsException(string? invalidTitle)
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var spotId = Guid.NewGuid();
    var points = 10;
    var dueAt = DateTime.UtcNow.AddHours(2);

    // Act & Assert
    Should.Throw<ArgumentException>(() =>
      new TaskInstance(CreateSpotWithFamily(), invalidTitle!, new(points), dueAt));
  }

  [Theory]
  [InlineData(0)]
  [InlineData(5)]
  public void Constructor_WithInvalidPoints_ThrowsException(int invalidPoints)
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var spotId = Guid.NewGuid();
    var title = "Feed the Spot";
    var dueAt = DateTime.UtcNow.AddHours(2);

    // Act & Assert
    Should.Throw<ArgumentException>(() =>
      new TaskInstance(CreateSpotWithFamily(), title, new(invalidPoints), dueAt));
  }

  [Theory]
  [InlineData(1)]
  [InlineData(2)]
  [InlineData(3)]
  [InlineData(4)]
  public void Constructor_WithValidPoints_CreatesTask(int validPoints)
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var spotId = Guid.NewGuid();
    var title = "Feed the Spot";
    var dueAt = DateTime.UtcNow.AddHours(2);

    // Act
    var spot = CreateSpotWithFamily();
    var task = new TaskInstance(spot, title, new(validPoints), dueAt);

    // Assert
    task.Points.Value.ShouldBe(validPoints);
  }

  [Fact]
  public void Start_WhenActive_ChangesStatusToInProgress()
  {
    // Arrange
    var spot = CreateSpotWithFamily();
    var family = spot.Family;
    var task = new TaskInstance(spot, "Feed the Spot", new(2), DateTime.UtcNow.AddHours(2));
    var user = new User(123, "Test User");
    var member = family.AddMember(user, FamilyRole.Admin);
    task.Status.ShouldBe(TaskStatus.Active);

    // Act
    var result = task.StartByUserId(user.Id, family);

    // Assert
    result.IsSuccess.ShouldBe(true);
    task.Status.ShouldBe(TaskStatus.InProgress);
    task.StartedByMemberId.ShouldBe(member.Id);
  }

  [Fact]
  public void Start_WhenAlreadyInProgress_RemainsInProgress()
  {
    // Arrange
    var spot = CreateSpotWithFamily();
    var family = spot.Family;
    var task = new TaskInstance(spot, "Feed the Spot", new(2), DateTime.UtcNow.AddHours(2));
    var user = new User(123, "Test User");
    var member = family.AddMember(user, FamilyRole.Admin);
    task.StartByMember(member);

    // Act
    task.StartByMember(member);

    // Assert
    task.Status.ShouldBe(TaskStatus.InProgress);
  }

  [Fact]
  public void Start_WhenCompleted_RemainsCompleted()
  {
    // Arrange

    var spot = CreateSpotWithFamily();
    var family = spot.Family;
    var task = new TaskInstance(spot, "Feed the Spot", new(2), DateTime.UtcNow.AddHours(2));
    var user = new User(123, "Test User");
    var member = family.AddMember(user, FamilyRole.Admin);
    var completedByMember = family.AddMember(user, FamilyRole.Admin);
    task.Complete(completedByMember, DateTime.UtcNow);

    // Act
    task.StartByMember(member);

    // Assert
    task.Status.ShouldBe(TaskStatus.Completed);
  }

  [Fact]
  public void Complete_WhenActive_CompletesTask()
  {
    // Arrange
    var spot = CreateSpotWithFamily();
    var task = new TaskInstance(spot, "Feed the Spot", new(2), DateTime.UtcNow.AddHours(2));
    var completedByMember = spot.Family.AddMember(new(0, "NewUser"), FamilyRole.Admin);
    var completedAt = DateTime.UtcNow;

    // Act
    task.Complete(completedByMember, completedAt);

    // Assert
    task.Status.ShouldBe(TaskStatus.Completed);
    task.CompletedByMemberId.ShouldBe(completedByMember.Id);
    task.CompletedAt.ShouldBe(completedAt);
  }

  [Fact]
  public void Complete_WhenInProgress_CompletesTask()
  {
    // Arrange
    var spot = CreateSpotWithFamily();
    var family = spot.Family;
    var task = new TaskInstance(spot, "Feed the Spot", new(2), DateTime.UtcNow.AddHours(2));
    var user = new User(123, "Test User");
    var member = family.AddMember(user, FamilyRole.Admin);
    task.StartByMember(member);
    var completedByMember = family.AddMember(user, FamilyRole.Admin);
    var completedAt = DateTime.UtcNow;

    // Act
    task.Complete(completedByMember, completedAt);

    // Assert
    task.Status.ShouldBe(TaskStatus.Completed);
    task.CompletedByMemberId.ShouldBe(completedByMember.Id);
    task.CompletedAt.ShouldBe(completedAt);
  }

  [Fact]
  public void Complete_RaisesTaskCompletedEvent()
  {
    // Arrange
    var spot = CreateSpotWithFamily();
    var task = new TaskInstance(spot, "Feed the Spot", new(2), DateTime.UtcNow.AddHours(2));
    var completedByMember = spot.Family.AddMember(new(0, "NewUser"), FamilyRole.Admin);
    var completedAt = DateTime.UtcNow;

    // Act
    task.Complete(completedByMember, completedAt);

    // Assert
    task.DomainEvents.ShouldContain(e => e is TaskCompletedEvent);
  }

  [Fact]
  public void Complete_WhenAlreadyCompleted_DoesNotChangeState()
  {
    // Arrange
    var spot = CreateSpotWithFamily();
    var task = new TaskInstance(spot, "Feed the Spot", new(2), DateTime.UtcNow.AddHours(2));
    var firstCompletedByMember = spot.Family.AddMember(new(0, "firstUser"), FamilyRole.Admin);
    var firstCompletedAt = DateTime.UtcNow;
    task.Complete(firstCompletedByMember, firstCompletedAt);

    var secondCompletedByMember = spot.Family.AddMember(new(0, "secondUser"), FamilyRole.Admin);
    ;
    var secondCompletedAt = DateTime.UtcNow.AddHours(1);

    // Act
    task.Complete(secondCompletedByMember, secondCompletedAt);

    // Assert
    task.Status.ShouldBe(TaskStatus.Completed);
    task.CompletedByMemberId.ShouldBe(firstCompletedByMember.Id);
    task.CompletedAt.ShouldBe(firstCompletedAt);
  }

  [Fact]
  public void Complete_WithNullMember_ThrowsException()
  {
    // Arrange
    var spot = CreateSpotWithFamily();
    var task = new TaskInstance(spot, "Feed the Spot", new(2), DateTime.UtcNow.AddHours(2));
    var completedAt = DateTime.UtcNow;

    // Act & Assert
    Should.Throw<ArgumentNullException>(() => task.Complete(null!, completedAt));
  }

  [Fact]
  public void TaskLifecycle_FromActiveToCompleted_WorksCorrectly()
  {
    // Arrange
    var spot = CreateSpotWithFamily();
    var task = new TaskInstance(spot, "Feed the Spot", new(2), DateTime.UtcNow.AddHours(2));
    var family = spot.Family;
    var completedByMember = family.AddMember(new(0, "CompetedUser"), FamilyRole.Admin);
    var completedAt = DateTime.UtcNow;

    // Act & Assert - Initial state
    task.Status.ShouldBe(TaskStatus.Active);
    task.CompletedByMember.ShouldBeNull();
    task.CompletedAt.ShouldBeNull();

    // Act & Assert - StartByMember
    var member = family.AddMember(new(1, "NewUser"), FamilyRole.Admin);

    task.StartByMember(member);
    task.Status.ShouldBe(TaskStatus.InProgress);

    // Act & Assert - Complete
    task.Complete(completedByMember, completedAt);
    task.Status.ShouldBe(TaskStatus.Completed);
    task.CompletedByMemberId.ShouldBe(completedByMember.Id);
    task.CompletedAt.ShouldBe(completedAt);
  }

  [Fact]
  public void TaskLifecycle_DirectlyCompleteWithoutStart_WorksCorrectly()
  {
    // Arrange
    var spot = CreateSpotWithFamily();
    var task = new TaskInstance(spot, "Feed the Spot", new(2), DateTime.UtcNow.AddHours(2));
    var completedByMember = spot.Family.AddMember(new(0, "user"), FamilyRole.Admin);
    var completedAt = DateTime.UtcNow;

    // Act
    task.Complete(completedByMember, completedAt);

    // Assert
    task.Status.ShouldBe(TaskStatus.Completed);
    task.CompletedByMemberId.ShouldBe(completedByMember.Id);
    task.CompletedAt.ShouldBe(completedAt);
  }
}
