using FamilyTaskManager.Core.PetAggregate;
using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.UseCases.Pets;

namespace FamilyTaskManager.UnitTests.UseCases.Pets;

public class CreatePetHandlerTests
{
  private readonly IRepository<Pet> _petRepository;
  private readonly IRepository<Family> _familyRepository;
  private readonly CreatePetHandler _handler;

  public CreatePetHandlerTests()
  {
    _petRepository = Substitute.For<IRepository<Pet>>();
    _familyRepository = Substitute.For<IRepository<Family>>();
    _handler = new CreatePetHandler(_petRepository, _familyRepository);
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
}
