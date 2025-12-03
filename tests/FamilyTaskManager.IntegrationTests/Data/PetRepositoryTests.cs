using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.PetAggregate;
using FamilyTaskManager.Core.TaskAggregate;

namespace FamilyTaskManager.IntegrationTests.Data;

public class PetRepositoryTests : RepositoryTestsBase<Pet>
{
  protected override Pet CreateTestEntity(string uniqueSuffix = "") =>
    CreatePetWithFamily(Guid.NewGuid(), PetType.Cat, $"Fluffy{uniqueSuffix}");

  protected override Pet CreateSecondTestEntity(string uniqueSuffix = "") =>
    CreatePetWithFamily(Guid.NewGuid(), PetType.Dog, $"Buddy{uniqueSuffix}");

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

  private Pet CreatePetWithFamily(Guid familyId, PetType type, string name)
  {
    var family = new Family($"Test Family {familyId:N}", "UTC");
    var familyRepository = GetRepository<Family>();

    familyRepository.AddAsync(family).GetAwaiter().GetResult();
    DbContext.SaveChangesAsync().GetAwaiter().GetResult();

    return new Pet(family.Id, type, name);
  }

  [Fact]
  public async Task Pet_ShouldHaveDefaultMoodScore()
  {
    // Arrange
    var pet = CreatePetWithFamily(Guid.NewGuid(), PetType.Hamster, "Hammy");

    // Act
    await Repository.AddAsync(pet);
    await DbContext.SaveChangesAsync();

    // Assert
    var retrieved = await Repository.GetByIdAsync(pet.Id);
    retrieved.ShouldNotBeNull();
    retrieved.MoodScore.ShouldBe(100);
    retrieved.Type.ShouldBe(PetType.Hamster);
  }

  [Fact]
  public async Task UpdateMoodScore_ShouldClampBetween0And100()
  {
    // Arrange
    var pet = CreatePetWithFamily(Guid.NewGuid(), PetType.Cat, "Test Cat");
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

  [Fact]
  public async Task DeleteAsync_ShouldRemovePet_WhenTasksExist()
  {
    // Arrange
    var family = new Family("Family With Tasks", "UTC");
    var familyRepository = GetRepository<Family>();
    await familyRepository.AddAsync(family);
    await DbContext.SaveChangesAsync();

    var pet = new Pet(family.Id, PetType.Cat, "Busy Cat");
    // Set navigation property for test
    typeof(Pet).GetProperty("Family")!.SetValue(pet, family);
    await Repository.AddAsync(pet);
    await DbContext.SaveChangesAsync();

    var task = new TaskInstance(
      pet,
      "Feed Busy Cat",
      new TaskPoints(2),
      TaskType.OneTime,
      DateTime.UtcNow.AddDays(1));

    var taskRepository = GetRepository<TaskInstance>();
    await taskRepository.AddAsync(task);
    await DbContext.SaveChangesAsync();

    // Act
    await Repository.DeleteAsync(pet);
    await DbContext.SaveChangesAsync();

    // Assert
    (await Repository.GetByIdAsync(pet.Id)).ShouldBeNull();
    (await taskRepository.GetByIdAsync(task.Id)).ShouldBeNull();
  }
}
