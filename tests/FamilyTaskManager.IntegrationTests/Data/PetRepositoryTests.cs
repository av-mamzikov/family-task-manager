using FamilyTaskManager.Core.PetAggregate;

namespace FamilyTaskManager.IntegrationTests.Data;

public class PetRepositoryTests : RepositoryTestsBase<Pet>
{
  protected override Pet CreateTestEntity(string uniqueSuffix = "") =>
    new(Guid.NewGuid(), PetType.Cat, $"Fluffy{uniqueSuffix}");

  protected override Pet CreateSecondTestEntity(string uniqueSuffix = "") =>
    new(Guid.NewGuid(), PetType.Dog, $"Buddy{uniqueSuffix}");

  protected override void ModifyEntity(Pet entity)
  {
    entity.UpdateName("Modified Pet Name");
    entity.UpdateMoodScore(75);
  }

  protected override void AssertEntityWasModified(Pet entity)
  {
    entity.Name.ShouldBe("Modified Pet Name");
    entity.MoodScore.ShouldBe(75);
  }

  [Fact]
  public async Task Pet_ShouldHaveDefaultMoodScore()
  {
    // Arrange
    var pet = new Pet(Guid.NewGuid(), PetType.Hamster, "Hammy");

    // Act
    await Repository.AddAsync(pet);
    await DbContext.SaveChangesAsync();

    // Assert
    var retrieved = await Repository.GetByIdAsync(pet.Id);
    retrieved.ShouldNotBeNull();
    retrieved.MoodScore.ShouldBe(50);
    retrieved.Type.ShouldBe(PetType.Hamster);
  }

  [Fact]
  public async Task UpdateMoodScore_ShouldClampBetween0And100()
  {
    // Arrange
    var pet = new Pet(Guid.NewGuid(), PetType.Cat, "Test Cat");
    await Repository.AddAsync(pet);
    await DbContext.SaveChangesAsync();

    // Act
    pet.UpdateMoodScore(150); // Should clamp to 100
    await Repository.UpdateAsync(pet);
    await DbContext.SaveChangesAsync();

    // Assert
    var retrieved = await Repository.GetByIdAsync(pet.Id);
    retrieved.ShouldNotBeNull();
    retrieved.MoodScore.ShouldBe(100);
  }
}
