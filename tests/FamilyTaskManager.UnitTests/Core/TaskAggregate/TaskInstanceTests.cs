using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.PetAggregate;
using FamilyTaskManager.Core.TaskAggregate;
using FamilyTaskManager.Core.TaskAggregate.Events;
using FamilyTaskManager.Core.UserAggregate;
using TaskStatus = FamilyTaskManager.Core.TaskAggregate.TaskStatus;

namespace FamilyTaskManager.UnitTests.Core.TaskAggregate;

public class TaskInstanceTests
{
  private static Pet CreatePetWithFamily(string petName = "Test Pet", string timezone = "UTC")
  {
    var family = new Family("Test Family", timezone);
    var pet = new Pet(family.Id, PetType.Cat, petName);
    // Manually set navigation property for tests
    typeof(Pet).GetProperty("Family")!.SetValue(pet, family);
    return pet;
  }

  private static FamilyMember CreateMemberWithUser(Guid? familyId = null, string userName = "Test User")
  {
    var user = new User(123456789L, userName);
    var family = new Family("Test Family", "UTC");
    var member = family.AddMember(user, FamilyRole.Child);
    // Manually set navigation property for tests
    typeof(FamilyMember).GetProperty("User")!.SetValue(member, user);
    return member;
  }

  [Fact]
  public void Constructor_WithValidParameters_CreatesTaskInstance()
  {
    // Arrange
    var pet = CreatePetWithFamily();
    var title = "Feed the pet";
    var points = 2;
    var type = TaskType.OneTime;
    var dueAt = DateTime.UtcNow.AddHours(2);

    // Act
    var task = new TaskInstance(pet, title, new TaskPoints(points), type, dueAt);

    // Assert
    task.FamilyId.ShouldBe(pet.FamilyId);
    task.PetId.ShouldBe(pet.Id);
    task.Title.ShouldBe(title);
    task.Points.Value.ShouldBe(points);
    task.Type.ShouldBe(type);
    task.DueAt.ShouldBe(dueAt);
    task.Status.ShouldBe(TaskStatus.Active);
    task.TemplateId.ShouldBeNull();
    task.CompletedByMember.ShouldBeNull();
    task.CompletedAt.ShouldBeNull();
    task.CreatedAt.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-1), DateTime.UtcNow.AddSeconds(1));
  }

  [Fact]
  public void Constructor_WithTemplateId_CreatesTaskInstanceWithTemplate()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var petId = Guid.NewGuid();
    var title = "Feed the pet";
    var points = 2;
    var type = TaskType.Recurring;
    var dueAt = DateTime.UtcNow.AddHours(2);
    var templateId = Guid.NewGuid();

    // Act
    var pet = CreatePetWithFamily();
    var task = new TaskInstance(pet, title, new TaskPoints(points), type, dueAt, templateId);

    // Assert
    task.TemplateId.ShouldBe(templateId);
    task.Type.ShouldBe(TaskType.Recurring);
  }

  [Fact]
  public void Constructor_WithWhitespace_TrimsTitle()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var petId = Guid.NewGuid();
    var title = "  Feed the pet  ";
    var points = 2;
    var type = TaskType.OneTime;
    var dueAt = DateTime.UtcNow.AddHours(2);

    // Act
    var pet = CreatePetWithFamily();
    var task = new TaskInstance(pet, title, new TaskPoints(points), type, dueAt);

    // Assert
    task.Title.ShouldBe("Feed the pet");
  }

  [Fact]
  public void Constructor_RaisesTaskCreatedEvent()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var petId = Guid.NewGuid();
    var title = "Feed the pet";
    var points = 2;
    var type = TaskType.OneTime;
    var dueAt = DateTime.UtcNow.AddHours(2);

    // Act
    var pet = CreatePetWithFamily();
    var task = new TaskInstance(pet, title, new TaskPoints(points), type, dueAt);

    // Assert
    task.DomainEvents.ShouldContain(e => e is TaskCreatedEvent);
  }

  [Fact]
  public void Constructor_WithEmptyFamilyId_ThrowsException()
  {
    // Arrange
    var familyId = Guid.Empty;
    var petId = Guid.NewGuid();
    var title = "Feed the pet";
    var points = 10;
    var type = TaskType.OneTime;
    var dueAt = DateTime.UtcNow.AddHours(2);

    // Act & Assert
    Should.Throw<ArgumentException>(() =>
      new TaskInstance(CreatePetWithFamily(), title, new TaskPoints(points), type, dueAt));
  }

  [Fact]
  public void Constructor_WithEmptyPetId_ThrowsException()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var petId = Guid.Empty;
    var title = "Feed the pet";
    var points = 10;
    var type = TaskType.OneTime;
    var dueAt = DateTime.UtcNow.AddHours(2);

    // Act & Assert
    Should.Throw<ArgumentException>(() =>
      new TaskInstance(CreatePetWithFamily(), title, new TaskPoints(points), type, dueAt));
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
    var type = TaskType.OneTime;
    var dueAt = DateTime.UtcNow.AddHours(2);

    // Act & Assert
    Should.Throw<ArgumentException>(() =>
      new TaskInstance(CreatePetWithFamily(), invalidTitle!, new TaskPoints(points), type, dueAt));
  }

  [Theory]
  [InlineData(0)]
  [InlineData(4)]
  public void Constructor_WithInvalidPoints_ThrowsException(int invalidPoints)
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var petId = Guid.NewGuid();
    var title = "Feed the pet";
    var type = TaskType.OneTime;
    var dueAt = DateTime.UtcNow.AddHours(2);

    // Act & Assert
    Should.Throw<ArgumentException>(() =>
      new TaskInstance(CreatePetWithFamily(), title, new TaskPoints(invalidPoints), type, dueAt));
  }

  [Theory]
  [InlineData(1)]
  [InlineData(2)]
  [InlineData(3)]
  public void Constructor_WithValidPoints_CreatesTask(int validPoints)
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var petId = Guid.NewGuid();
    var title = "Feed the pet";
    var type = TaskType.OneTime;
    var dueAt = DateTime.UtcNow.AddHours(2);

    // Act
    var pet = CreatePetWithFamily();
    var task = new TaskInstance(pet, title, new TaskPoints(validPoints), type, dueAt);

    // Assert
    task.Points.Value.ShouldBe(validPoints);
  }

  [Theory]
  [InlineData(TaskType.OneTime)]
  [InlineData(TaskType.Recurring)]
  public void Constructor_WithDifferentTypes_CreatesWithCorrectType(TaskType type)
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var petId = Guid.NewGuid();
    var title = "Feed the pet";
    var points = 2;
    var dueAt = DateTime.UtcNow.AddHours(2);

    // Act
    var pet = CreatePetWithFamily();
    var task = new TaskInstance(pet, title, new TaskPoints(points), type, dueAt);

    // Assert
    task.Type.ShouldBe(type);
  }

  [Fact]
  public void Start_WhenActive_ChangesStatusToInProgress()
  {
    // Arrange
    var pet = CreatePetWithFamily();
    var task = new TaskInstance(pet, "Feed the pet", new TaskPoints(2), TaskType.OneTime, DateTime.UtcNow.AddHours(2));
    var memberId = Guid.NewGuid();
    task.Status.ShouldBe(TaskStatus.Active);

    // Act
    task.Start(memberId);

    // Assert
    task.Status.ShouldBe(TaskStatus.InProgress);
    task.StartedByMemberId.ShouldBe(memberId);
  }

  [Fact]
  public void Start_WhenAlreadyInProgress_RemainsInProgress()
  {
    // Arrange
    var pet = CreatePetWithFamily();
    var task = new TaskInstance(pet, "Feed the pet", new TaskPoints(2), TaskType.OneTime, DateTime.UtcNow.AddHours(2));
    var memberId = Guid.NewGuid();
    task.Start(memberId);

    // Act
    task.Start(memberId);

    // Assert
    task.Status.ShouldBe(TaskStatus.InProgress);
  }

  [Fact]
  public void Start_WhenCompleted_RemainsCompleted()
  {
    // Arrange
    var pet = CreatePetWithFamily();
    var task = new TaskInstance(pet, "Feed the pet", new TaskPoints(2), TaskType.OneTime, DateTime.UtcNow.AddHours(2));
    var memberId = Guid.NewGuid();
    var completedByMember = CreateMemberWithUser();
    task.Complete(completedByMember, DateTime.UtcNow);

    // Act
    task.Start(memberId);

    // Assert
    task.Status.ShouldBe(TaskStatus.Completed);
  }

  [Fact]
  public void Complete_WhenActive_CompletesTask()
  {
    // Arrange
    var pet = CreatePetWithFamily();
    var task = new TaskInstance(pet, "Feed the pet", new TaskPoints(2), TaskType.OneTime, DateTime.UtcNow.AddHours(2));
    var completedByMember = CreateMemberWithUser();
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
    var pet = CreatePetWithFamily();
    var task = new TaskInstance(pet, "Feed the pet", new TaskPoints(2), TaskType.OneTime, DateTime.UtcNow.AddHours(2));
    var memberId = Guid.NewGuid();
    task.Start(memberId);
    var completedByMember = CreateMemberWithUser();
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
    var pet = CreatePetWithFamily();
    var task = new TaskInstance(pet, "Feed the pet", new TaskPoints(2), TaskType.OneTime, DateTime.UtcNow.AddHours(2));
    var completedByMember = CreateMemberWithUser();
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
    var pet = CreatePetWithFamily();
    var task = new TaskInstance(pet, "Feed the pet", new TaskPoints(2), TaskType.OneTime, DateTime.UtcNow.AddHours(2));
    var firstCompletedByMember = CreateMemberWithUser();
    var firstCompletedAt = DateTime.UtcNow;
    task.Complete(firstCompletedByMember, firstCompletedAt);

    var secondCompletedByMember = CreateMemberWithUser();
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
    var pet = CreatePetWithFamily();
    var task = new TaskInstance(pet, "Feed the pet", new TaskPoints(2), TaskType.OneTime, DateTime.UtcNow.AddHours(2));
    var completedAt = DateTime.UtcNow;

    // Act & Assert
    Should.Throw<ArgumentNullException>(() => task.Complete(null!, completedAt));
  }

  [Fact]
  public void TaskLifecycle_FromActiveToCompleted_WorksCorrectly()
  {
    // Arrange
    var pet = CreatePetWithFamily();
    var task = new TaskInstance(pet, "Feed the pet", new TaskPoints(2), TaskType.OneTime, DateTime.UtcNow.AddHours(2));
    var completedByMember = CreateMemberWithUser();
    var completedAt = DateTime.UtcNow;

    // Act & Assert - Initial state
    task.Status.ShouldBe(TaskStatus.Active);
    task.CompletedByMember.ShouldBeNull();
    task.CompletedAt.ShouldBeNull();

    // Act & Assert - Start
    var memberId = Guid.NewGuid();
    task.Start(memberId);
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
    var pet = CreatePetWithFamily();
    var task = new TaskInstance(pet, "Feed the pet", new TaskPoints(2), TaskType.OneTime, DateTime.UtcNow.AddHours(2));
    var completedByMember = CreateMemberWithUser();
    var completedAt = DateTime.UtcNow;

    // Act
    task.Complete(completedByMember, completedAt);

    // Assert
    task.Status.ShouldBe(TaskStatus.Completed);
    task.CompletedByMemberId.ShouldBe(completedByMember.Id);
    task.CompletedAt.ShouldBe(completedAt);
  }
}
