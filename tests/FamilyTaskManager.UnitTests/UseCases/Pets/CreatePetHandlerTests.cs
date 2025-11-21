using FamilyTaskManager.Core.PetAggregate;
using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.TaskAggregate;
using FamilyTaskManager.UseCases.Pets;

namespace FamilyTaskManager.UnitTests.UseCases.Pets;

public class CreatePetHandlerTests
{
  private readonly IRepository<Pet> _petRepository;
  private readonly IRepository<Family> _familyRepository;
  private readonly IRepository<TaskTemplate> _taskTemplateRepository;
  private readonly CreatePetHandler _handler;

  public CreatePetHandlerTests()
  {
    _petRepository = Substitute.For<IRepository<Pet>>();
    _familyRepository = Substitute.For<IRepository<Family>>();
    _taskTemplateRepository = Substitute.For<IRepository<TaskTemplate>>();
    _handler = new CreatePetHandler(_petRepository, _familyRepository, _taskTemplateRepository);
  }

  [Fact]
  public async Task Handle_ValidCommand_CreatesPet()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var family = new Family("Smith Family", "UTC", true);
    var command = new CreatePetCommand(familyId, PetType.Cat, "Fluffy");
    
    _familyRepository.GetByIdAsync(familyId, Arg.Any<CancellationToken>())
      .Returns(family);
    
    Pet? capturedPet = null;
    await _petRepository.AddAsync(Arg.Do<Pet>(p => capturedPet = p), Arg.Any<CancellationToken>());

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    capturedPet.ShouldNotBeNull();
    result.Value.ShouldBe(capturedPet.Id);
    capturedPet.FamilyId.ShouldBe(familyId);
    capturedPet.Type.ShouldBe(PetType.Cat);
    capturedPet.Name.ShouldBe("Fluffy");
    capturedPet.MoodScore.ShouldBe(50);
  }

  [Fact]
  public async Task Handle_NonExistentFamily_ReturnsNotFound()
  {
    // Arrange
    var command = new CreatePetCommand(Guid.NewGuid(), PetType.Dog, "Buddy");
    
    _familyRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
      .Returns((Family?)null);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeFalse();
    result.Status.ShouldBe(Ardalis.Result.ResultStatus.NotFound);
  }

  [Theory]
  [InlineData("A")] // Too short
  [InlineData("")] // Empty
  [InlineData("   ")] // Whitespace
  public async Task Handle_InvalidName_ReturnsInvalid(string name)
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var family = new Family("Smith Family", "UTC", true);
    var command = new CreatePetCommand(familyId, PetType.Cat, name);
    
    _familyRepository.GetByIdAsync(familyId, Arg.Any<CancellationToken>())
      .Returns(family);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeFalse();
    result.Status.ShouldBe(Ardalis.Result.ResultStatus.Invalid);
  }

  [Theory]
  [InlineData(PetType.Cat)]
  [InlineData(PetType.Dog)]
  [InlineData(PetType.Hamster)]
  public async Task Handle_DifferentPetTypes_CreatesCorrectType(PetType petType)
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var family = new Family("Smith Family", "UTC", true);
    var command = new CreatePetCommand(familyId, petType, "Pet Name");
    
    _familyRepository.GetByIdAsync(familyId, Arg.Any<CancellationToken>())
      .Returns(family);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    await _petRepository.Received(1).AddAsync(
      Arg.Is<Pet>(p => p.Type == petType),
      Arg.Any<CancellationToken>());
  }

  [Theory]
  [InlineData(PetType.Cat, 8)] // Cat has 8 default templates
  [InlineData(PetType.Dog, 6)] // Dog has 6 default templates
  [InlineData(PetType.Hamster, 5)] // Hamster has 5 default templates
  public async Task Handle_ValidCommand_CreatesDefaultTaskTemplates(PetType petType, int expectedTemplateCount)
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var family = new Family("Smith Family", "UTC", true);
    var command = new CreatePetCommand(familyId, petType, "Pet Name");
    
    _familyRepository.GetByIdAsync(familyId, Arg.Any<CancellationToken>())
      .Returns(family);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    await _taskTemplateRepository.Received(expectedTemplateCount).AddAsync(
      Arg.Any<TaskTemplate>(),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_CreatesCatPet_CreatesCorrectTaskTemplates()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var family = new Family("Smith Family", "UTC", true);
    var command = new CreatePetCommand(familyId, PetType.Cat, "Fluffy");
    
    _familyRepository.GetByIdAsync(familyId, Arg.Any<CancellationToken>())
      .Returns(family);

    var capturedTemplates = new List<TaskTemplate>();
    await _taskTemplateRepository.AddAsync(
      Arg.Do<TaskTemplate>(t => capturedTemplates.Add(t)),
      Arg.Any<CancellationToken>());

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    capturedTemplates.Count.ShouldBe(8);
    
    // Verify some key templates
    capturedTemplates.ShouldContain(t => t.Title == "Убрать какахи из лотка кота" && t.Points == 4);
    capturedTemplates.ShouldContain(t => t.Title == "Налить свежую воду коту" && t.Points == 3);
    capturedTemplates.ShouldContain(t => t.Title == "Поиграть с котом" && t.Points == 2);
    
    // All templates should belong to the same family and pet
    capturedTemplates.ShouldAllBe(t => t.FamilyId == familyId);
    capturedTemplates.ShouldAllBe(t => t.IsActive);
    capturedTemplates.ShouldAllBe(t => t.CreatedBy == new Guid("00000000-0000-0000-0000-000000000001")); // System-created
  }

  [Fact]
  public async Task Handle_CreatesDogPet_CreatesCorrectTaskTemplates()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var family = new Family("Smith Family", "UTC", true);
    var command = new CreatePetCommand(familyId, PetType.Dog, "Buddy");
    
    _familyRepository.GetByIdAsync(familyId, Arg.Any<CancellationToken>())
      .Returns(family);

    var capturedTemplates = new List<TaskTemplate>();
    await _taskTemplateRepository.AddAsync(
      Arg.Do<TaskTemplate>(t => capturedTemplates.Add(t)),
      Arg.Any<CancellationToken>());

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    capturedTemplates.Count.ShouldBe(6);
    
    // Verify some key templates
    capturedTemplates.ShouldContain(t => t.Title == "Выгулять собаку утром" && t.Points == 5);
    capturedTemplates.ShouldContain(t => t.Title == "Выгулять собаку вечером" && t.Points == 5);
    capturedTemplates.ShouldContain(t => t.Title == "Искупать собаку" && t.Points == 8);
  }

  [Fact]
  public async Task Handle_CreatesHamsterPet_CreatesCorrectTaskTemplates()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var family = new Family("Smith Family", "UTC", true);
    var command = new CreatePetCommand(familyId, PetType.Hamster, "Nibbles");
    
    _familyRepository.GetByIdAsync(familyId, Arg.Any<CancellationToken>())
      .Returns(family);

    var capturedTemplates = new List<TaskTemplate>();
    await _taskTemplateRepository.AddAsync(
      Arg.Do<TaskTemplate>(t => capturedTemplates.Add(t)),
      Arg.Any<CancellationToken>());

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    capturedTemplates.Count.ShouldBe(5);
    
    // Verify some key templates
    capturedTemplates.ShouldContain(t => t.Title == "Насыпать корм хомяку" && t.Points == 2);
    capturedTemplates.ShouldContain(t => t.Title == "Убрать клетку хомяка" && t.Points == 5);
    capturedTemplates.ShouldContain(t => t.Title == "Полностью помыть клетку хомяка" && t.Points == 7);
  }
}
