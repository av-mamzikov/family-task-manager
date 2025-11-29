using Ardalis.Result;
using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.PetAggregate;
using FamilyTaskManager.Core.Services;
using FamilyTaskManager.Core.TaskAggregate;
using FamilyTaskManager.UseCases.Families.Specifications;
using FamilyTaskManager.UseCases.Tasks;
using TaskStatus = FamilyTaskManager.Core.TaskAggregate.TaskStatus;

namespace FamilyTaskManager.UnitTests.UseCases.Tasks;

public class CompleteTaskHandlerTests
{
  private readonly IRepository<Family> _familyRepository;
  private readonly CompleteTaskHandler _handler;
  private readonly IPetMoodCalculator _moodCalculator;
  private readonly IRepository<Pet> _petRepository;
  private readonly IRepository<TaskInstance> _taskRepository;

  public CompleteTaskHandlerTests()
  {
    _taskRepository = Substitute.For<IRepository<TaskInstance>>();
    _familyRepository = Substitute.For<IRepository<Family>>();
    _petRepository = Substitute.For<IRepository<Pet>>();
    _moodCalculator = Substitute.For<IPetMoodCalculator>();
    _handler = new CompleteTaskHandler(_taskRepository, _familyRepository, _petRepository, _moodCalculator);
  }

  [Fact]
  public async Task Handle_ValidCommand_CompletesTaskAndAddsPoints()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var familyId = Guid.NewGuid();
    var taskId = Guid.NewGuid();
    var petId = Guid.NewGuid();

    var task = new TaskInstance(familyId, petId, "Feed the cat", 10, TaskType.OneTime, DateTime.UtcNow.AddDays(1));
    var family = new Family("Smith Family", "UTC");
    family.AddMember(userId, FamilyRole.Child);

    var command = new CompleteTaskCommand(taskId, userId);

    _taskRepository.GetByIdAsync(taskId, Arg.Any<CancellationToken>())
      .Returns(task);
    _familyRepository.FirstOrDefaultAsync(Arg.Any<GetFamilyWithMembersSpec>(), Arg.Any<CancellationToken>())
      .Returns(family);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    task.Status.ShouldBe(TaskStatus.Completed);
    task.CompletedByMember?.UserId.ShouldBe(userId);
    task.CompletedAt.ShouldNotBeNull();

    var member = family.Members.First();
    member.Points.ShouldBe(10);

    await _taskRepository.Received(1).UpdateAsync(task, Arg.Any<CancellationToken>());
    await _familyRepository.Received(1).UpdateAsync(family, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_NonExistentTask_ReturnsNotFound()
  {
    // Arrange
    var command = new CompleteTaskCommand(Guid.NewGuid(), Guid.NewGuid());

    _taskRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
      .Returns((TaskInstance?)null);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeFalse();
    result.Status.ShouldBe(ResultStatus.NotFound);
  }

  [Fact]
  public async Task Handle_AlreadyCompletedTask_ReturnsError()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var familyId = Guid.NewGuid();
    var taskId = Guid.NewGuid();
    var petId = Guid.NewGuid();

    var task = new TaskInstance(familyId, petId, "Feed the cat", 10, TaskType.OneTime, DateTime.UtcNow.AddDays(1));
    task.Complete(userId, DateTime.UtcNow);

    var command = new CompleteTaskCommand(taskId, userId);

    _taskRepository.GetByIdAsync(taskId, Arg.Any<CancellationToken>())
      .Returns(task);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeFalse();
    result.Status.ShouldBe(ResultStatus.Error);
  }

  [Fact]
  public async Task Handle_UserNotInFamily_ReturnsError()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var differentUserId = Guid.NewGuid();
    var familyId = Guid.NewGuid();
    var taskId = Guid.NewGuid();
    var petId = Guid.NewGuid();

    var task = new TaskInstance(familyId, petId, "Feed the cat", 10, TaskType.OneTime, DateTime.UtcNow.AddDays(1));
    var family = new Family("Smith Family", "UTC");
    family.AddMember(differentUserId, FamilyRole.Child); // Different user

    var command = new CompleteTaskCommand(taskId, userId);

    _taskRepository.GetByIdAsync(taskId, Arg.Any<CancellationToken>())
      .Returns(task);
    _familyRepository.FirstOrDefaultAsync(Arg.Any<GetFamilyWithMembersSpec>(), Arg.Any<CancellationToken>())
      .Returns(family);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeFalse();
    result.Status.ShouldBe(ResultStatus.Error);
  }
}
