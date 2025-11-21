using FamilyTaskManager.Core.TaskAggregate;

namespace FamilyTaskManager.IntegrationTests.Data;

public class TaskTemplateRepositoryTests : BaseRepositoryTestFixture
{
  private IRepository<TaskTemplate> Repository => GetRepository<TaskTemplate>();

  [Fact]
  public async Task AddAsync_ShouldPersistTaskTemplateToDatabase()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var petId = Guid.NewGuid();
    var createdBy = Guid.NewGuid();
    var taskTemplate = new TaskTemplate(familyId, petId, "Feed the cat", 10, "0 8 * * *", createdBy);

    // Act
    await Repository.AddAsync(taskTemplate);
    await DbContext.SaveChangesAsync();

    // Assert
    var retrieved = await Repository.GetByIdAsync(taskTemplate.Id);
    retrieved.ShouldNotBeNull();
    retrieved.Title.ShouldBe("Feed the cat");
    retrieved.Points.ShouldBe(10);
    retrieved.Schedule.ShouldBe("0 8 * * *");
    retrieved.FamilyId.ShouldBe(familyId);
    retrieved.PetId.ShouldBe(petId);
    retrieved.CreatedBy.ShouldBe(createdBy);
    retrieved.IsActive.ShouldBeTrue();
  }

  [Fact]
  public async Task UpdateAsync_ShouldModifyExistingTaskTemplate()
  {
    // Arrange
    var taskTemplate = new TaskTemplate(
      Guid.NewGuid(), 
      Guid.NewGuid(), 
      "Original Title", 
      5, 
      "0 9 * * *", 
      Guid.NewGuid()
    );
    await Repository.AddAsync(taskTemplate);
    await DbContext.SaveChangesAsync();

    // Act
    taskTemplate.Update("Updated Title", 15, "0 10 * * *");
    await Repository.UpdateAsync(taskTemplate);
    await DbContext.SaveChangesAsync();

    // Assert
    var retrieved = await Repository.GetByIdAsync(taskTemplate.Id);
    retrieved.ShouldNotBeNull();
    retrieved.Title.ShouldBe("Updated Title");
    retrieved.Points.ShouldBe(15);
    retrieved.Schedule.ShouldBe("0 10 * * *");
  }

  [Fact]
  public async Task Deactivate_ShouldSetIsActiveToFalse()
  {
    // Arrange
    var taskTemplate = new TaskTemplate(
      Guid.NewGuid(), 
      Guid.NewGuid(), 
      "Task to Deactivate", 
      10, 
      "0 8 * * *", 
      Guid.NewGuid()
    );
    await Repository.AddAsync(taskTemplate);
    await DbContext.SaveChangesAsync();

    // Act
    taskTemplate.Deactivate();
    await Repository.UpdateAsync(taskTemplate);
    await DbContext.SaveChangesAsync();

    // Assert
    var retrieved = await Repository.GetByIdAsync(taskTemplate.Id);
    retrieved.ShouldNotBeNull();
    retrieved.IsActive.ShouldBeFalse();
  }

  [Fact]
  public async Task DeleteAsync_ShouldRemoveTaskTemplateFromDatabase()
  {
    // Arrange
    var taskTemplate = new TaskTemplate(
      Guid.NewGuid(), 
      Guid.NewGuid(), 
      "Task to Delete", 
      10, 
      "0 8 * * *", 
      Guid.NewGuid()
    );
    await Repository.AddAsync(taskTemplate);
    await DbContext.SaveChangesAsync();

    // Act
    await Repository.DeleteAsync(taskTemplate);
    await DbContext.SaveChangesAsync();

    // Assert
    var retrieved = await Repository.GetByIdAsync(taskTemplate.Id);
    retrieved.ShouldBeNull();
  }

  [Fact]
  public async Task ListAsync_ShouldReturnAllTaskTemplates()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var petId = Guid.NewGuid();
    var createdBy = Guid.NewGuid();

    var task1 = new TaskTemplate(familyId, petId, "Task 1", 10, "0 8 * * *", createdBy);
    var task2 = new TaskTemplate(familyId, petId, "Task 2", 15, "0 9 * * *", createdBy);
    var task3 = new TaskTemplate(familyId, petId, "Task 3", 20, "0 10 * * *", createdBy);

    await Repository.AddRangeAsync([task1, task2, task3]);
    await DbContext.SaveChangesAsync();

    // Act
    var tasks = await Repository.ListAsync();

    // Assert
    tasks.Count.ShouldBe(3);
    tasks.ShouldContain(t => t.Title == "Task 1");
    tasks.ShouldContain(t => t.Title == "Task 2");
    tasks.ShouldContain(t => t.Title == "Task 3");
  }

  [Fact]
  public async Task CountAsync_ShouldReturnCorrectCount()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var petId = Guid.NewGuid();
    var createdBy = Guid.NewGuid();

    await Repository.AddRangeAsync([
      new TaskTemplate(familyId, petId, "Task 1", 10, "0 8 * * *", createdBy),
      new TaskTemplate(familyId, petId, "Task 2", 15, "0 9 * * *", createdBy)
    ]);
    await DbContext.SaveChangesAsync();

    // Act
    var count = await Repository.CountAsync();

    // Assert
    count.ShouldBe(2);
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
  public async Task AnyAsync_WithExistingData_ShouldReturnTrue()
  {
    // Arrange
    var taskTemplate = new TaskTemplate(
      Guid.NewGuid(), 
      Guid.NewGuid(), 
      "Test Task", 
      10, 
      "0 8 * * *", 
      Guid.NewGuid()
    );
    await Repository.AddAsync(taskTemplate);
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
}
