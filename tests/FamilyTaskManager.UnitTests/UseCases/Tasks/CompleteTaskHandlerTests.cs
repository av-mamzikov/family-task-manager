using FamilyTaskManager.Core.TaskAggregate;
using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.UseCases.Tasks;
using FamilyTaskManager.UseCases.Families.Specifications;
using TaskStatus = FamilyTaskManager.Core.TaskAggregate.TaskStatus;

namespace FamilyTaskManager.UnitTests.UseCases.Tasks;

public class CompleteTaskHandlerTests
{
  private readonly IRepository<TaskInstance> _taskRepository;
  private readonly IRepository<Family> _familyRepository;
  private readonly CompleteTaskHandler _handler;

  public CompleteTaskHandlerTests()
  {
    _taskRepository = Substitute.For<IRepository<TaskInstance>>();
    _familyRepository = Substitute.For<IRepository<Family>>();
    _handler = new CompleteTaskHandler(_taskRepository, _familyRepository);
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
    var family = new Family("Smith Family", "UTC", true);
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
    task.CompletedBy.ShouldBe(userId);
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
    result.Status.ShouldBe(Ardalis.Result.ResultStatus.NotFound);
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
    result.Status.ShouldBe(Ardalis.Result.ResultStatus.Error);
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
    var family = new Family("Smith Family", "UTC", true);
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
    result.Status.ShouldBe(Ardalis.Result.ResultStatus.Error);
  }
}
