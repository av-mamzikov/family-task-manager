using FamilyTaskManager.Core.FamilyAggregate;

namespace FamilyTaskManager.IntegrationTests.Data;

public class FamilyRepositoryTests : BaseRepositoryTestFixture
{
  private IRepository<Family> Repository => GetRepository<Family>();

  [Fact]
  public async Task AddAsync_ShouldPersistFamilyToDatabase()
  {
    // Arrange
    var family = new Family("Test Family", "Europe/Moscow");

    // Act
    await Repository.AddAsync(family);
    await DbContext.SaveChangesAsync();

    // Assert
    var retrieved = await Repository.GetByIdAsync(family.Id);
    retrieved.ShouldNotBeNull();
    retrieved.Name.ShouldBe("Test Family");
    retrieved.Timezone.ShouldBe("Europe/Moscow");
    retrieved.LeaderboardEnabled.ShouldBeTrue();
  }

  [Fact]
  public async Task UpdateAsync_ShouldModifyExistingFamily()
  {
    // Arrange
    var family = new Family("Original Name", "UTC");
    await Repository.AddAsync(family);
    await DbContext.SaveChangesAsync();

    // Act
    family.UpdateSettings(false, "Europe/London");
    await Repository.UpdateAsync(family);
    await DbContext.SaveChangesAsync();

    // Assert
    var retrieved = await Repository.GetByIdAsync(family.Id);
    retrieved.ShouldNotBeNull();
    retrieved.LeaderboardEnabled.ShouldBeFalse();
    retrieved.Timezone.ShouldBe("Europe/London");
  }

  [Fact]
  public async Task DeleteAsync_ShouldRemoveFamilyFromDatabase()
  {
    // Arrange
    var family = new Family("Family To Delete", "UTC");
    await Repository.AddAsync(family);
    await DbContext.SaveChangesAsync();

    // Act
    await Repository.DeleteAsync(family);
    await DbContext.SaveChangesAsync();

    // Assert
    var retrieved = await Repository.GetByIdAsync(family.Id);
    retrieved.ShouldBeNull();
  }

  [Fact]
  public async Task GetByIdAsync_WithNonExistentId_ShouldReturnNull()
  {
    // Arrange
    var nonExistentId = Guid.NewGuid();

    // Act
    var result = await Repository.GetByIdAsync(nonExistentId);

    // Assert
    result.ShouldBeNull();
  }

  [Fact]
  public async Task ListAsync_ShouldReturnAllFamilies()
  {
    // Arrange
    var family1 = new Family("Family 1", "UTC");
    var family2 = new Family("Family 2", "Europe/Moscow");
    var family3 = new Family("Family 3", "America/New_York");

    await Repository.AddRangeAsync([family1, family2, family3]);
    await DbContext.SaveChangesAsync();

    // Act
    var families = await Repository.ListAsync();

    // Assert
    families.Count.ShouldBe(3);
    families.ShouldContain(f => f.Name == "Family 1");
    families.ShouldContain(f => f.Name == "Family 2");
    families.ShouldContain(f => f.Name == "Family 3");
  }

  [Fact]
  public async Task AddMember_ShouldPersistWithFamily()
  {
    // Arrange
    var family = new Family("Family With Members", "UTC");
    await Repository.AddAsync(family);
    await DbContext.SaveChangesAsync();
    
    var userId = Guid.NewGuid();
    
    // Act
    var member = family.AddMember(userId, family.Id, FamilyRole.Admin);
    await Repository.UpdateAsync(family);
    await DbContext.SaveChangesAsync();

    // Assert
    var retrieved = await Repository.GetByIdAsync(family.Id);
    retrieved.ShouldNotBeNull();
    retrieved.Members.Count.ShouldBe(1);
    retrieved.Members.First().UserId.ShouldBe(userId);
    retrieved.Members.First().Role.ShouldBe(FamilyRole.Admin);
  }

  [Fact]
  public async Task CountAsync_ShouldReturnCorrectCount()
  {
    // Arrange
    await Repository.AddRangeAsync([
      new Family("Family 1", "UTC"),
      new Family("Family 2", "UTC"),
      new Family("Family 3", "UTC")
    ]);
    await DbContext.SaveChangesAsync();

    // Act
    var count = await Repository.CountAsync();

    // Assert
    count.ShouldBe(3);
  }

  [Fact]
  public async Task AnyAsync_WithExistingData_ShouldReturnTrue()
  {
    // Arrange
    var family = new Family("Test Family", "UTC");
    await Repository.AddAsync(family);
    await DbContext.SaveChangesAsync();

    // Act
    var exists = await Repository.AnyAsync();

    // Assert
    exists.ShouldBeTrue();
  }

  [Fact]
  public async Task AnyAsync_WithEmptyDatabase_ShouldReturnFalse()
  {
    // Act
    var exists = await Repository.AnyAsync();

    // Assert
    exists.ShouldBeFalse();
  }

  [Fact]
  public async Task SaveChangesAsync_ShouldPersistMultipleOperations()
  {
    // Arrange
    var family1 = new Family("Family 1", "UTC");
    var family2 = new Family("Family 2", "UTC");

    // Act
    await Repository.AddAsync(family1);
    await Repository.AddAsync(family2);
    await DbContext.SaveChangesAsync();

    // Assert - verify both families were persisted
    var allFamilies = await Repository.ListAsync();
    allFamilies.Count.ShouldBe(2);
    allFamilies.ShouldContain(f => f.Name == "Family 1");
    allFamilies.ShouldContain(f => f.Name == "Family 2");
  }
}
