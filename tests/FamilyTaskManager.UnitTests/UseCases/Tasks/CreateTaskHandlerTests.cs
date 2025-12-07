using Ardalis.Result;
using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.Services;
using FamilyTaskManager.Core.SpotAggregate;
using FamilyTaskManager.Core.TaskAggregate;
using FamilyTaskManager.UseCases.Spots.Specifications;
using FamilyTaskManager.UseCases.Tasks;

namespace FamilyTaskManager.UnitTests.UseCases.Tasks;

public class CreateTaskHandlerTests
{
  private readonly IAppRepository<Family> _familyAppRepository;
  private readonly CreateTaskHandler _handler;
  private readonly ISpotMoodCalculator _moodCalculator;
  private readonly IAppRepository<Spot> _SpotAppRepository;
  private readonly IAppRepository<TaskInstance> _taskAppRepository;
  private readonly ITimeZoneService _timeZoneService;

  public CreateTaskHandlerTests()
  {
    _taskAppRepository = Substitute.For<IAppRepository<TaskInstance>>();
    _SpotAppRepository = Substitute.For<IAppRepository<Spot>>();
    _familyAppRepository = Substitute.For<IAppRepository<Family>>();
    _timeZoneService = Substitute.For<ITimeZoneService>();
    _moodCalculator = Substitute.For<ISpotMoodCalculator>();
    _handler = new(_taskAppRepository, _SpotAppRepository, _timeZoneService,
      _moodCalculator);
  }

  [Fact]
  public async Task Handle_ValidCommand_CreatesTask()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var SpotId = Guid.NewGuid();
    var family = new Family("Test Family", "UTC", false);
    var Spot = new Spot(familyId, SpotType.Cat, "Fluffy");
    // Set Family navigation property for test
    typeof(Spot).GetProperty("Family")!.SetValue(Spot, family);
    var dueAt = DateTime.UtcNow.AddDays(1);
    var command = new CreateTaskCommand(familyId, SpotId, "Feed the cat", new(2), dueAt, Guid.NewGuid());

    _SpotAppRepository.FirstOrDefaultAsync(Arg.Any<GetSpotByIdWithFamilySpec>(), Arg.Any<CancellationToken>())
      .Returns(Spot);
    _familyAppRepository.GetByIdAsync(familyId, Arg.Any<CancellationToken>())
      .Returns(family);
    _timeZoneService.ConvertToUtc(Arg.Any<DateTime>(), "UTC")
      .Returns(callInfo => callInfo.ArgAt<DateTime>(0));

    TaskInstance? capturedTask = null;
    await _taskAppRepository.AddAsync(Arg.Do<TaskInstance>(t => capturedTask = t), Arg.Any<CancellationToken>());

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    capturedTask.ShouldNotBeNull();
    result.Value.ShouldBe(capturedTask.Id);
    capturedTask.FamilyId.ShouldBe(familyId);
    capturedTask.SpotId.ShouldBe(Spot.Id);
    capturedTask.Title.ShouldBe("Feed the cat");
    capturedTask.Points.Value.ShouldBe(2);
    capturedTask.DueAt.ShouldBe(dueAt);
  }

  [Fact]
  public async Task Handle_NonExistentSpot_ReturnsNotFound()
  {
    // Arrange
    var command = new CreateTaskCommand(Guid.NewGuid(), Guid.NewGuid(), "Feed the cat", new(2),
      DateTime.UtcNow,
      Guid.NewGuid());

    _SpotAppRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
      .Returns((Spot?)null);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeFalse();
    result.Status.ShouldBe(ResultStatus.NotFound);
  }

  [Fact]
  public async Task Handle_SpotFromDifferentFamily_ReturnsError()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var differentFamilyId = Guid.NewGuid();
    var SpotId = Guid.NewGuid();
    var differentFamily = new Family("Different Family", "UTC", false);
    var Spot = new Spot(differentFamilyId, SpotType.Cat, "Fluffy");
    // Set Family navigation property for test
    typeof(Spot).GetProperty("Family")!.SetValue(Spot, differentFamily);
    var family = new Family("Test Family", "UTC", false);
    var command = new CreateTaskCommand(familyId, SpotId, "Feed the cat", new(2), DateTime.UtcNow,
      Guid.NewGuid());

    _SpotAppRepository.FirstOrDefaultAsync(Arg.Any<GetSpotByIdWithFamilySpec>(), Arg.Any<CancellationToken>())
      .Returns(Spot);
    _familyAppRepository.GetByIdAsync(familyId, Arg.Any<CancellationToken>())
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
    var SpotId = Guid.NewGuid();
    var family = new Family("Test Family", "UTC", false);
    var Spot = new Spot(familyId, SpotType.Cat, "Fluffy");
    // Set Family navigation property for test
    typeof(Spot).GetProperty("Family")!.SetValue(Spot, family);
    var command =
      new CreateTaskCommand(familyId, SpotId, title, new(points), DateTime.UtcNow, Guid.NewGuid());

    _SpotAppRepository.FirstOrDefaultAsync(Arg.Any<GetSpotByIdWithFamilySpec>(), Arg.Any<CancellationToken>())
      .Returns(Spot);
    _familyAppRepository.GetByIdAsync(familyId, Arg.Any<CancellationToken>())
      .Returns(family);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeFalse();
    result.Status.ShouldBe(ResultStatus.Invalid);
  }

  [Theory]
  [InlineData(0)]
  [InlineData(5)]
  public void TaskPoints_Constructor_WithInvalidPoints_ThrowsException(int points) =>
    // Act & Assert
    Should.Throw<ArgumentException>(() => new TaskPoints(points));
}
