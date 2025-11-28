using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.PetAggregate;
using FamilyTaskManager.Core.TaskAggregate;

namespace FamilyTaskManager.IntegrationTests.Data;

public class TaskTemplateRepositoryTests : BaseRepositoryTestFixture
{
  private IRepository<TaskTemplate> Repository => GetRepository<TaskTemplate>();

  private async Task<TaskTemplate> CreateTaskTemplateWithDependencies(string title = "Feed the cat", int points = 10,
    string schedule = "0 8 * * *")
  {
    // Создаем семью
    var family = new Family($"Test Family {Guid.NewGuid():N}", "UTC");
    var familyRepository = GetRepository<Family>();
    await familyRepository.AddAsync(family);
    await DbContext.SaveChangesAsync();

    // Создаем питомца для этой семьи
    var pet = new Pet(family.Id, PetType.Cat, "Test Pet");
    var petRepository = GetRepository<Pet>();
    await petRepository.AddAsync(pet);
    await DbContext.SaveChangesAsync();

    // Создаем шаблон задачи с валидными ID
    var createdBy = Guid.NewGuid();
    return new TaskTemplate(family.Id, pet.Id, title, points, schedule, TimeSpan.FromHours(12), createdBy);
  }

  [Fact]
  public async Task AddAsync_ShouldPersistTaskTemplateToDatabase()
  {
    // Arrange
    var taskTemplate = await CreateTaskTemplateWithDependencies();

    // Act
    await Repository.AddAsync(taskTemplate);
    await DbContext.SaveChangesAsync();

    // Assert
    var retrieved = await Repository.GetByIdAsync(taskTemplate.Id);
    retrieved.ShouldNotBeNull();
    retrieved.Title.ShouldBe("Feed the cat");
    retrieved.Points.ShouldBe(10);
    retrieved.Schedule.ShouldBe("0 8 * * *");
    retrieved.IsActive.ShouldBeTrue();
  }

  [Fact]
  public async Task UpdateAsync_ShouldModifyExistingTaskTemplate()
  {
    // Arrange
    var taskTemplate = await CreateTaskTemplateWithDependencies("Original Title", 5, "0 9 * * *");
    await Repository.AddAsync(taskTemplate);
    await DbContext.SaveChangesAsync();

    // Act
    taskTemplate.Update("Updated Title", 15, "0 10 * * *", TimeSpan.FromHours(12));
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
    var taskTemplate = await CreateTaskTemplateWithDependencies("Task to Deactivate");
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
    var taskTemplate = await CreateTaskTemplateWithDependencies("Task to Delete");
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
    var task1 = await CreateTaskTemplateWithDependencies("Task 1");
    var task2 = await CreateTaskTemplateWithDependencies("Task 2", 15, "0 9 * * *");
    var task3 = await CreateTaskTemplateWithDependencies("Task 3", 20, "0 10 * * *");

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
    var task1 = await CreateTaskTemplateWithDependencies("Task 1");
    var task2 = await CreateTaskTemplateWithDependencies("Task 2", 15, "0 9 * * *");

    await Repository.AddRangeAsync([task1, task2]);
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
    var taskTemplate = await CreateTaskTemplateWithDependencies("Test Task");
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
