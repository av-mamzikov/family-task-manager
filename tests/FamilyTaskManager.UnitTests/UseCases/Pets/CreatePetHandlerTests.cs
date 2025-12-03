using Ardalis.Result;
using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.PetAggregate;
using FamilyTaskManager.Core.TaskAggregate;
using FamilyTaskManager.UseCases.Pets;

namespace FamilyTaskManager.UnitTests.UseCases.Pets;

public class CreatePetHandlerTests
{
  private readonly IAppRepository<Family> _familyAppRepository;
  private readonly CreatePetHandler _handler;
  private readonly IAppRepository<Pet> _petAppRepository;
  private readonly IAppRepository<TaskTemplate> _taskTemplateAppRepository;

  public CreatePetHandlerTests()
  {
    _petAppRepository = Substitute.For<IAppRepository<Pet>>();
    _familyAppRepository = Substitute.For<IAppRepository<Family>>();
    _taskTemplateAppRepository = Substitute.For<IAppRepository<TaskTemplate>>();
    _handler = new(_petAppRepository, _familyAppRepository, _taskTemplateAppRepository);
  }

  [Fact]
  public async Task Handle_ValidCommand_CreatesPet()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var family = new Family("Smith Family", "UTC");
    var command = new CreatePetCommand(familyId, PetType.Cat, "Fluffy");

    _familyAppRepository.GetByIdAsync(familyId, Arg.Any<CancellationToken>())
      .Returns(family);

    Pet? capturedPet = null;
    await _petAppRepository.AddAsync(Arg.Do<Pet>(p => capturedPet = p), Arg.Any<CancellationToken>());

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    capturedPet.ShouldNotBeNull();
    result.Value.ShouldBe(capturedPet.Id);
    capturedPet.FamilyId.ShouldBe(familyId);
    capturedPet.Type.ShouldBe(PetType.Cat);
    capturedPet.Name.ShouldBe("Fluffy");
    capturedPet.MoodScore.ShouldBe(100);
  }

  [Fact]
  public async Task Handle_NonExistentFamily_ReturnsNotFound()
  {
    // Arrange
    var command = new CreatePetCommand(Guid.NewGuid(), PetType.Dog, "Buddy");

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
    var command = new CreatePetCommand(familyId, PetType.Cat, name);

    _familyAppRepository.GetByIdAsync(familyId, Arg.Any<CancellationToken>())
      .Returns(family);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeFalse();
    result.Status.ShouldBe(ResultStatus.Invalid);
  }

  [Theory]
  [InlineData(PetType.Cat)]
  [InlineData(PetType.Dog)]
  [InlineData(PetType.Hamster)]
  [InlineData(PetType.Parrot)]
  public async Task Handle_DifferentPetTypes_CreatesCorrectType(PetType petType)
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var family = new Family("Smith Family", "UTC");
    var command = new CreatePetCommand(familyId, petType, "Pet Name");

    _familyAppRepository.GetByIdAsync(familyId, Arg.Any<CancellationToken>())
      .Returns(family);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    await _petAppRepository.Received(1).AddAsync(
      Arg.Is<Pet>(p => p.Type == petType),
      Arg.Any<CancellationToken>());
  }

  [Theory]
  [InlineData(PetType.Cat, 8)] // Cat has 8 default templates
  [InlineData(PetType.Dog, 6)] // Dog has 6 default templates
  [InlineData(PetType.Hamster, 5)] // Hamster has 5 default templates
  [InlineData(PetType.Parrot, 6)] // Parrot has 6 default templates
  public async Task Handle_ValidCommand_CreatesDefaultTaskTemplates(PetType petType, int expectedTemplateCount)
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var family = new Family("Smith Family", "UTC");
    var command = new CreatePetCommand(familyId, petType, "Pet Name");

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
  public async Task Handle_CreatesCatPet_CreatesCorrectTaskTemplates()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var family = new Family("Smith Family", "UTC");
    var command = new CreatePetCommand(familyId, PetType.Cat, "Fluffy");

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

    // All templates should belong to the same family and pet
    capturedTemplates.ShouldAllBe(t => t.FamilyId == familyId);
    capturedTemplates.ShouldAllBe(t =>
      t.CreatedBy == new Guid("00000000-0000-0000-0000-000000000001")); // System-created
  }

  [Fact]
  public async Task Handle_CreatesDogPet_CreatesCorrectTaskTemplates()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var family = new Family("Smith Family", "UTC");
    var command = new CreatePetCommand(familyId, PetType.Dog, "Buddy");

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
  public async Task Handle_CreatesHamsterPet_CreatesCorrectTaskTemplates()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var family = new Family("Smith Family", "UTC");
    var command = new CreatePetCommand(familyId, PetType.Hamster, "Nibbles");

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
  public async Task Handle_CreatesParrotPet_CreatesCorrectTaskTemplates()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var family = new Family("Smith Family", "UTC");
    var command = new CreatePetCommand(familyId, PetType.Parrot, "Polly");

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
