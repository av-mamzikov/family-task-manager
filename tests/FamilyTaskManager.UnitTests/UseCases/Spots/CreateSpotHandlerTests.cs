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
  private readonly IAppRepository<Spot> _SpotAppRepository;
  private readonly IAppRepository<TaskTemplate> _taskTemplateAppRepository;

  public CreateSpotHandlerTests()
  {
    _SpotAppRepository = Substitute.For<IAppRepository<Spot>>();
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

    Spot? capturedSpot = null;
    await _SpotAppRepository.AddAsync(Arg.Do<Spot>(p => capturedSpot = p), Arg.Any<CancellationToken>());

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
      Arg.Is<Spot>(p => p.Type == spotType),
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

  [Fact]
  public async Task Handle_CreatesCatSpot_CreatesCorrectTaskTemplates()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var family = new Family("Smith Family", "UTC");
    var command = new CreateSpotCommand(familyId, SpotType.Cat, "Fluffy");

    _familyAppRepository.GetByIdAsync(familyId, Arg.Any<CancellationToken>())
      .Returns(family);

    var capturedTemplates = new List<TaskTemplate>();
    await _taskTemplateAppRepository.AddAsync(
      Arg.Do<TaskTemplate>(t => capturedTemplates.Add(t)),
      Arg.Any<CancellationToken>());

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    capturedTemplates.Count.ShouldBe(8);

    // Verify some key templates
    capturedTemplates.ShouldContain(t => t.Title == "Убрать какахи из лотка кота" && t.Points.Value == 3);
    capturedTemplates.ShouldContain(t => t.Title == "Налить свежую воду коту" && t.Points.Value == 2);
    capturedTemplates.ShouldContain(t => t.Title == "Поиграть с котом" && t.Points.Value == 1);

    // All templates should belong to the same family and Spot
    capturedTemplates.ShouldAllBe(t => t.FamilyId == familyId);
    capturedTemplates.ShouldAllBe(t =>
      t.CreatedBy == new Guid("00000000-0000-0000-0000-000000000001")); // System-created
  }

  [Fact]
  public async Task Handle_CreatesDogSpot_CreatesCorrectTaskTemplates()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var family = new Family("Smith Family", "UTC");
    var command = new CreateSpotCommand(familyId, SpotType.Dog, "Buddy");

    _familyAppRepository.GetByIdAsync(familyId, Arg.Any<CancellationToken>())
      .Returns(family);

    var capturedTemplates = new List<TaskTemplate>();
    await _taskTemplateAppRepository.AddAsync(
      Arg.Do<TaskTemplate>(t => capturedTemplates.Add(t)),
      Arg.Any<CancellationToken>());

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    capturedTemplates.Count.ShouldBe(6);

    // Verify some key templates
    capturedTemplates.ShouldContain(t => t.Title == "Выгулять собаку утром" && t.Points.Value == 3);
    capturedTemplates.ShouldContain(t => t.Title == "Выгулять собаку вечером" && t.Points.Value == 3);
    capturedTemplates.ShouldContain(t => t.Title == "Искупать собаку" && t.Points.Value == 3);
  }

  [Fact]
  public async Task Handle_CreatesHamsterSpot_CreatesCorrectTaskTemplates()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var family = new Family("Smith Family", "UTC");
    var command = new CreateSpotCommand(familyId, SpotType.Hamster, "Nibbles");

    _familyAppRepository.GetByIdAsync(familyId, Arg.Any<CancellationToken>())
      .Returns(family);

    var capturedTemplates = new List<TaskTemplate>();
    await _taskTemplateAppRepository.AddAsync(
      Arg.Do<TaskTemplate>(t => capturedTemplates.Add(t)),
      Arg.Any<CancellationToken>());

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    capturedTemplates.Count.ShouldBe(5);

    // Verify some key templates
    capturedTemplates.ShouldContain(t => t.Title == "Насыпать корм хомяку" && t.Points.Value == 2);
    capturedTemplates.ShouldContain(t => t.Title == "Убрать клетку хомяка" && t.Points.Value == 3);
    capturedTemplates.ShouldContain(t => t.Title == "Полностью помыть клетку хомяка" && t.Points.Value == 3);
  }

  [Fact]
  public async Task Handle_CreatesParrotSpot_CreatesCorrectTaskTemplates()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var family = new Family("Smith Family", "UTC");
    var command = new CreateSpotCommand(familyId, SpotType.Parrot, "Polly");

    _familyAppRepository.GetByIdAsync(familyId, Arg.Any<CancellationToken>())
      .Returns(family);

    var capturedTemplates = new List<TaskTemplate>();
    await _taskTemplateAppRepository.AddAsync(
      Arg.Do<TaskTemplate>(t => capturedTemplates.Add(t)),
      Arg.Any<CancellationToken>());

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    capturedTemplates.Count.ShouldBe(6);

    // Verify some key templates
    capturedTemplates.ShouldContain(t => t.Title == "Покормить попугая" && t.Points.Value == 2);
    capturedTemplates.ShouldContain(t => t.Title == "Поменять воду попугаю" && t.Points.Value == 2);
    capturedTemplates.ShouldContain(t => t.Title == "Полностью помыть клетку попугая" && t.Points.Value == 3);
  }
}
