using Ardalis.Result;
using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.Interfaces;
using FamilyTaskManager.Core.PetAggregate;
using FamilyTaskManager.Core.Services;
using FamilyTaskManager.Core.TaskAggregate;
using FamilyTaskManager.UseCases.Pets.Specifications;
using FamilyTaskManager.UseCases.Tasks;

namespace FamilyTaskManager.UnitTests.UseCases.Tasks;

public class CreateTaskHandlerTests
{
  private readonly IRepository<Family> _familyRepository;
  private readonly CreateTaskHandler _handler;
  private readonly IPetMoodCalculator _moodCalculator;
  private readonly IRepository<Pet> _petRepository;
  private readonly IRepository<TaskInstance> _taskRepository;
  private readonly ITimeZoneService _timeZoneService;

  public CreateTaskHandlerTests()
  {
    _taskRepository = Substitute.For<IRepository<TaskInstance>>();
    _petRepository = Substitute.For<IRepository<Pet>>();
    _familyRepository = Substitute.For<IRepository<Family>>();
    _timeZoneService = Substitute.For<ITimeZoneService>();
    _moodCalculator = Substitute.For<IPetMoodCalculator>();
    _handler = new CreateTaskHandler(_taskRepository, _petRepository, _timeZoneService,
      _moodCalculator);
  }

  [Fact]
  public async Task Handle_ValidCommand_CreatesTask()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var petId = Guid.NewGuid();
    var family = new Family("Test Family", "UTC", false);
    var pet = new Pet(familyId, PetType.Cat, "Fluffy");
    // Set Family navigation property for test
    typeof(Pet).GetProperty("Family")!.SetValue(pet, family);
    var dueAt = DateTime.UtcNow.AddDays(1);
    var command = new CreateTaskCommand(familyId, petId, "Feed the cat", new TaskPoints(2), dueAt, Guid.NewGuid());

    _petRepository.FirstOrDefaultAsync(Arg.Any<GetPetByIdWithFamilySpec>(), Arg.Any<CancellationToken>())
      .Returns(pet);
    _familyRepository.GetByIdAsync(familyId, Arg.Any<CancellationToken>())
      .Returns(family);
    _timeZoneService.ConvertToUtc(Arg.Any<DateTime>(), "UTC")
      .Returns(callInfo => callInfo.ArgAt<DateTime>(0));

    TaskInstance? capturedTask = null;
    await _taskRepository.AddAsync(Arg.Do<TaskInstance>(t => capturedTask = t), Arg.Any<CancellationToken>());

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    capturedTask.ShouldNotBeNull();
    result.Value.ShouldBe(capturedTask.Id);
    capturedTask.FamilyId.ShouldBe(familyId);
    capturedTask.PetId.ShouldBe(pet.Id);
    capturedTask.Title.ShouldBe("Feed the cat");
    capturedTask.Points.Value.ShouldBe(2);
    capturedTask.Type.ShouldBe(TaskType.OneTime);
  }

  [Fact]
  public async Task Handle_NonExistentPet_ReturnsNotFound()
  {
    // Arrange
    var command = new CreateTaskCommand(Guid.NewGuid(), Guid.NewGuid(), "Feed the cat", new TaskPoints(2),
      DateTime.UtcNow,
      Guid.NewGuid());

    _petRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
      .Returns((Pet?)null);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeFalse();
    result.Status.ShouldBe(ResultStatus.NotFound);
  }

  [Fact]
  public async Task Handle_PetFromDifferentFamily_ReturnsError()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var differentFamilyId = Guid.NewGuid();
    var petId = Guid.NewGuid();
    var differentFamily = new Family("Different Family", "UTC", false);
    var pet = new Pet(differentFamilyId, PetType.Cat, "Fluffy");
    // Set Family navigation property for test
    typeof(Pet).GetProperty("Family")!.SetValue(pet, differentFamily);
    var family = new Family("Test Family", "UTC", false);
    var command = new CreateTaskCommand(familyId, petId, "Feed the cat", new TaskPoints(2), DateTime.UtcNow,
      Guid.NewGuid());

    _petRepository.FirstOrDefaultAsync(Arg.Any<GetPetByIdWithFamilySpec>(), Arg.Any<CancellationToken>())
      .Returns(pet);
    _familyRepository.GetByIdAsync(familyId, Arg.Any<CancellationToken>())
      .Returns(family);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeFalse();
    result.Status.ShouldBe(ResultStatus.Error);
  }

  [Theory]
  [InlineData("AB", 1)] // Too short
  [InlineData("", 1)] // Empty
  public async Task Handle_InvalidTitle_ReturnsInvalid(string title, int points)
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var petId = Guid.NewGuid();
    var family = new Family("Test Family", "UTC", false);
    var pet = new Pet(familyId, PetType.Cat, "Fluffy");
    // Set Family navigation property for test
    typeof(Pet).GetProperty("Family")!.SetValue(pet, family);
    var command =
      new CreateTaskCommand(familyId, petId, title, new TaskPoints(points), DateTime.UtcNow, Guid.NewGuid());

    _petRepository.FirstOrDefaultAsync(Arg.Any<GetPetByIdWithFamilySpec>(), Arg.Any<CancellationToken>())
      .Returns(pet);
    _familyRepository.GetByIdAsync(familyId, Arg.Any<CancellationToken>())
      .Returns(family);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeFalse();
    result.Status.ShouldBe(ResultStatus.Invalid);
  }

  [Theory]
  [InlineData(0)]
  [InlineData(4)]
  public void TaskPoints_Constructor_WithInvalidPoints_ThrowsException(int points) =>
    // Act & Assert
    Should.Throw<ArgumentException>(() => new TaskPoints(points));
}
