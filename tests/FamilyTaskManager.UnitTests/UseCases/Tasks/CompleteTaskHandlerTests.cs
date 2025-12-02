using Ardalis.Result;
using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.PetAggregate;
using FamilyTaskManager.Core.Services;
using FamilyTaskManager.Core.TaskAggregate;
using FamilyTaskManager.UnitTests.Helpers;
using FamilyTaskManager.UseCases.Families.Specifications;
using FamilyTaskManager.UseCases.Tasks;
using TaskStatus = FamilyTaskManager.Core.TaskAggregate.TaskStatus;

namespace FamilyTaskManager.UnitTests.UseCases.Tasks;

public class CompleteTaskHandlerTests
{
  private readonly IAppRepository<Family> _familyAppRepository;
  private readonly CompleteTaskHandler _handler;
  private readonly IPetMoodCalculator _moodCalculator;
  private readonly IAppRepository<Pet> _petAppRepository;
  private readonly IAppRepository<TaskInstance> _taskAppRepository;

  public CompleteTaskHandlerTests()
  {
    _taskAppRepository = Substitute.For<IAppRepository<TaskInstance>>();
    _familyAppRepository = Substitute.For<IAppRepository<Family>>();
    _petAppRepository = Substitute.For<IAppRepository<Pet>>();
    _moodCalculator = Substitute.For<IPetMoodCalculator>();
    _handler = new(_taskAppRepository, _familyAppRepository, _petAppRepository, _moodCalculator);
  }

  [Fact]
  public async Task Handle_ValidCommand_CompletesTaskAndAddsPoints()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var taskId = Guid.NewGuid();

    var pet = TestHelpers.CreatePetWithFamily();
    var task = new TaskInstance(pet, "Feed the cat", new TaskPoints(2), TaskType.OneTime,
      DateTime.UtcNow.AddDays(1));
    var family = new Family("Smith Family", "UTC");
    var user = TestHelpers.CreateUser();
    var member = family.AddMember(user, FamilyRole.Child);
    // Set User navigation property for test
    typeof(FamilyMember).GetProperty("User")!.SetValue(member, user);
    userId = user.Id;

    var command = new CompleteTaskCommand(taskId, userId);

    _taskAppRepository.GetByIdAsync(taskId, Arg.Any<CancellationToken>())
      .Returns(task);
    _familyAppRepository.FirstOrDefaultAsync(Arg.Any<GetFamilyWithMembersAndUsersSpec>(), Arg.Any<CancellationToken>())
      .Returns(family);
    _petAppRepository.GetByIdAsync(task.PetId, Arg.Any<CancellationToken>())
      .Returns(pet);
    _moodCalculator.CalculateMoodScoreAsync(task.PetId, Arg.Any<CancellationToken>())
      .Returns(80);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    task.Status.ShouldBe(TaskStatus.Completed);
    task.CompletedByMember?.UserId.ShouldBe(userId);
    task.CompletedAt.ShouldNotBeNull();

    var familyMember = family.Members.First();
    familyMember.Points.ShouldBe(2);

    await _taskAppRepository.Received(1).UpdateAsync(task, Arg.Any<CancellationToken>());
    await _familyAppRepository.Received(1).UpdateAsync(family, Arg.Any<CancellationToken>());
    await _taskAppRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    await _petAppRepository.Received(1).GetByIdAsync(task.PetId, Arg.Any<CancellationToken>());
    await _moodCalculator.Received(1)
      .CalculateMoodScoreAsync(task.PetId, Arg.Any<CancellationToken>());
    await _petAppRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    pet.MoodScore.ShouldBe(80);
  }

  [Fact]
  public async Task Handle_NonExistentTask_ReturnsNotFound()
  {
    // Arrange
    var command = new CompleteTaskCommand(Guid.NewGuid(), Guid.NewGuid());

    _taskAppRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
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
    var taskId = Guid.NewGuid();

    var pet = TestHelpers.CreatePetWithFamily();
    var task = new TaskInstance(pet, "Feed the cat", new TaskPoints(2), TaskType.OneTime,
      DateTime.UtcNow.AddDays(1));
    var member = TestHelpers.CreateMemberWithUser();
    task.Complete(member, DateTime.UtcNow);

    var command = new CompleteTaskCommand(taskId, userId);

    _taskAppRepository.GetByIdAsync(taskId, Arg.Any<CancellationToken>())
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
    var taskId = Guid.NewGuid();

    var pet = TestHelpers.CreatePetWithFamily();
    var task = new TaskInstance(pet, "Feed the cat", new TaskPoints(2), TaskType.OneTime,
      DateTime.UtcNow.AddDays(1));
    var family = new Family("Smith Family", "UTC");
    var differentUser = TestHelpers.CreateUser();
    family.AddMember(differentUser, FamilyRole.Child); // Different user

    var command = new CompleteTaskCommand(taskId, userId);

    _taskAppRepository.GetByIdAsync(taskId, Arg.Any<CancellationToken>())
      .Returns(task);
    _familyAppRepository.FirstOrDefaultAsync(Arg.Any<GetFamilyWithMembersAndUsersSpec>(), Arg.Any<CancellationToken>())
      .Returns(family);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeFalse();
    result.Status.ShouldBe(ResultStatus.Error);
  }
}
