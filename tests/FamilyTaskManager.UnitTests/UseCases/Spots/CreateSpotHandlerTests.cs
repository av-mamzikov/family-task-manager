using Ardalis.Result;
using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.SpotAggregate;
using FamilyTaskManager.Core.TaskAggregate;
using FamilyTaskManager.UseCases.Spots;

namespace FamilyTaskManager.UnitTests.UseCases.Spots;

public class CreateSpotHandlerTests
{
  private readonly IAppRepository<Family> _familyAppRepository;
  private readonly CreateSpotHandler _handler;
  private readonly IAppRepository<SpotBowsing> _SpotAppRepository;
  private readonly IAppRepository<TaskTemplate> _taskTemplateAppRepository;

  public CreateSpotHandlerTests()
  {
    _SpotAppRepository = Substitute.For<IAppRepository<SpotBowsing>>();
    _familyAppRepository = Substitute.For<IAppRepository<Family>>();
    _taskTemplateAppRepository = Substitute.For<IAppRepository<TaskTemplate>>();
    _handler = new(_SpotAppRepository, _familyAppRepository, _taskTemplateAppRepository);
  }

  [Fact]
  public async Task Handle_ValidCommand_CreatesSpot()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var family = new Family("Smith Family", "UTC");
    var command = new CreateSpotCommand(familyId, SpotType.Cat, "Fluffy");

    _familyAppRepository.GetByIdAsync(familyId, Arg.Any<CancellationToken>())
      .Returns(family);

    SpotBowsing? capturedSpot = null;
    await _SpotAppRepository.AddAsync(Arg.Do<SpotBowsing>(p => capturedSpot = p), Arg.Any<CancellationToken>());

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    capturedSpot.ShouldNotBeNull();
    result.Value.ShouldBe(capturedSpot.Id);
    capturedSpot.FamilyId.ShouldBe(familyId);
    capturedSpot.Type.ShouldBe(SpotType.Cat);
    capturedSpot.Name.ShouldBe("Fluffy");
    capturedSpot.MoodScore.ShouldBe(100);
  }

  [Fact]
  public async Task Handle_NonExistentFamily_ReturnsNotFound()
  {
    // Arrange
    var command = new CreateSpotCommand(Guid.NewGuid(), SpotType.Dog, "Buddy");

    _familyAppRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
      .Returns((Family?)null);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeFalse();
    result.Status.ShouldBe(ResultStatus.NotFound);
  }

  [Theory]
  [InlineData("A")] // Too short
  [InlineData("")] // Empty
  [InlineData("   ")] // Whitespace
  public async Task Handle_InvalidName_ReturnsInvalid(string name)
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var family = new Family("Smith Family", "UTC");
    var command = new CreateSpotCommand(familyId, SpotType.Cat, name);

    _familyAppRepository.GetByIdAsync(familyId, Arg.Any<CancellationToken>())
      .Returns(family);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeFalse();
    result.Status.ShouldBe(ResultStatus.Invalid);
  }

  [Theory]
  [InlineData(SpotType.Cat)]
  [InlineData(SpotType.Dog)]
  [InlineData(SpotType.Hamster)]
  [InlineData(SpotType.Parrot)]
  public async Task Handle_DifferentSpotTypes_CreatesCorrectType(SpotType spotType)
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var family = new Family("Smith Family", "UTC");
    var command = new CreateSpotCommand(familyId, spotType, "Spot Name");

    _familyAppRepository.GetByIdAsync(familyId, Arg.Any<CancellationToken>())
      .Returns(family);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    await _SpotAppRepository.Received(1).AddAsync(
      Arg.Is<SpotBowsing>(p => p.Type == spotType),
      Arg.Any<CancellationToken>());
  }

  [Theory]
  [InlineData(SpotType.Cat, 8)] // Cat has 8 default templates
  [InlineData(SpotType.Dog, 6)] // Dog has 6 default templates
  [InlineData(SpotType.Hamster, 5)] // Hamster has 5 default templates
  [InlineData(SpotType.Parrot, 6)] // Parrot has 6 default templates
  public async Task Handle_ValidCommand_CreatesDefaultTaskTemplates(SpotType spotType, int expectedTemplateCount)
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var family = new Family("Smith Family", "UTC");
    var command = new CreateSpotCommand(familyId, spotType, "Spot Name");

    _familyAppRepository.GetByIdAsync(familyId, Arg.Any<CancellationToken>())
      .Returns(family);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    await _taskTemplateAppRepository.Received(expectedTemplateCount).AddAsync(
      Arg.Any<TaskTemplate>(),
      Arg.Any<CancellationToken>());
  }
}
