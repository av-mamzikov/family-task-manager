using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.SpotAggregate;
using FamilyTaskManager.Core.TaskAggregate;

namespace FamilyTaskManager.IntegrationTests.Data;

public class SpotRepositoryTests : RepositoryTestsBase<Spot>
{
  protected override Spot CreateTestEntity(string uniqueSuffix = "") =>
    CreateSpotWithFamily(Guid.NewGuid(), SpotType.Cat, $"Fluffy{uniqueSuffix}");

  protected override Spot CreateSecondTestEntity(string uniqueSuffix = "") =>
    CreateSpotWithFamily(Guid.NewGuid(), SpotType.Dog, $"Buddy{uniqueSuffix}");

  protected override void ModifyEntity(Spot entity)
  {
    entity.UpdateName("Modified Spot Name");
    entity.UpdateMoodScore(75);
  }

  protected override void AssertEntityWasModified(Spot entity)
  {
    entity.Name.ShouldBe("Modified Spot Name");
    entity.MoodScore.ShouldBe(75);
  }

  private Spot CreateSpotWithFamily(Guid familyId, SpotType type, string name)
  {
    var family = new Family($"Test Family {familyId:N}", "UTC");
    var familyRepository = GetRepository<Family>();

    familyRepository.AddAsync(family).GetAwaiter().GetResult();
    DbContext.SaveChangesAsync().GetAwaiter().GetResult();

    return new(family.Id, type, name);
  }

  [Fact]
  public async Task Spot_ShouldHaveDefaultMoodScore()
  {
    // Arrange
    var Spot = CreateSpotWithFamily(Guid.NewGuid(), SpotType.Hamster, "Hammy");

    // Act
    await Repository.AddAsync(Spot);
    await DbContext.SaveChangesAsync();

    // Assert
    var retrieved = await Repository.GetByIdAsync(Spot.Id);
    retrieved.ShouldNotBeNull();
    retrieved.MoodScore.ShouldBe(100);
    retrieved.Type.ShouldBe(SpotType.Hamster);
  }

  [Fact]
  public async Task UpdateMoodScore_ShouldClampBetween0And100()
  {
    // Arrange
    var Spot = CreateSpotWithFamily(Guid.NewGuid(), SpotType.Cat, "Test Cat");
    await Repository.AddAsync(Spot);
    await DbContext.SaveChangesAsync();

    // Act
    Spot.UpdateMoodScore(150); // Should clamp to 100
    await Repository.UpdateAsync(Spot);
    await DbContext.SaveChangesAsync();

    // Assert
    var retrieved = await Repository.GetByIdAsync(Spot.Id);
    retrieved.ShouldNotBeNull();
    retrieved.MoodScore.ShouldBe(100);
  }

  [Fact]
  public async Task DeleteAsync_ShouldRemoveSpot_WhenTasksExist()
  {
    // Arrange
    var family = new Family("Family With Tasks", "UTC");
    var familyRepository = GetRepository<Family>();
    await familyRepository.AddAsync(family);
    await DbContext.SaveChangesAsync();

    var Spot = new Spot(family.Id, SpotType.Cat, "Busy Cat");
    // Set navigation property for test
    typeof(Spot).GetProperty("Family")!.SetValue(Spot, family);
    await Repository.AddAsync(Spot);
    await DbContext.SaveChangesAsync();

    var task = new TaskInstance(
      Spot,
      "Feed Busy Cat",
      new(2),
      DateTime.UtcNow.AddDays(1));

    var taskRepository = GetRepository<TaskInstance>();
    await taskRepository.AddAsync(task);
    await DbContext.SaveChangesAsync();

    // Act
    await Repository.DeleteAsync(Spot);
    await DbContext.SaveChangesAsync();

    // Assert
    (await Repository.GetByIdAsync(Spot.Id)).ShouldBeNull();
    (await taskRepository.GetByIdAsync(task.Id)).ShouldBeNull();
  }
}
