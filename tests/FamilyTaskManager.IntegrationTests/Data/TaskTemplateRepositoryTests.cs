using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.SpotAggregate;
using FamilyTaskManager.Core.TaskAggregate;

namespace FamilyTaskManager.IntegrationTests.Data;

public class TaskTemplateRepositoryTests : BaseRepositoryTestFixture
{
  private IRepository<TaskTemplate> Repository => GetRepository<TaskTemplate>();

  private async Task<TaskTemplate> CreateTaskTemplateWithDependencies(string title = "Feed the cat", int points = 2,
    Schedule? schedule = null)
  {
    // Создаем семью
    var family = new Family($"Test Family {Guid.NewGuid():N}", "UTC");
    var familyRepository = GetRepository<Family>();
    await familyRepository.AddAsync(family);
    await DbContext.SaveChangesAsync();

    // Создаем спота для этой семьи
    var Spot = new Spot(family.Id, SpotType.Cat, "Test Spot");
    var SpotRepository = GetRepository<Spot>();
    await SpotRepository.AddAsync(Spot);
    await DbContext.SaveChangesAsync();

    // Создаем шаблон задачи с валидными ID
    var createdBy = Guid.NewGuid();
    var defaultSchedule = schedule ?? Schedule.CreateDaily(new TimeOnly(8, 0)).Value;
    return new TaskTemplate(family.Id, Spot.Id, title, new(points), defaultSchedule, TimeSpan.FromHours(12),
      createdBy);
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
    retrieved.Points.Value.ShouldBe(2);
    retrieved.Schedule.Type.ShouldBe(ScheduleType.Daily);
    retrieved.Schedule.Time.ShouldBe(new TimeOnly(8, 0));
  }

  [Fact]
  public async Task UpdateAsync_ShouldModifyExistingTaskTemplate()
  {
    // Arrange
    var originalSchedule = Schedule.CreateDaily(new TimeOnly(9, 0)).Value;
    var taskTemplate = await CreateTaskTemplateWithDependencies("Original Title", 2, originalSchedule);
    await Repository.AddAsync(taskTemplate);
    await DbContext.SaveChangesAsync();

    // Act
    var updatedSchedule = Schedule.CreateDaily(new TimeOnly(10, 0)).Value;
    taskTemplate.Update("Updated Title", new TaskPoints(3), updatedSchedule, TimeSpan.FromHours(12));
    await Repository.UpdateAsync(taskTemplate);
    await DbContext.SaveChangesAsync();

    // Assert
    var retrieved = await Repository.GetByIdAsync(taskTemplate.Id);
    retrieved.ShouldNotBeNull();
    retrieved.Title.ShouldBe("Updated Title");
    retrieved.Points.Value.ShouldBe(3);
    retrieved.Schedule.Type.ShouldBe(ScheduleType.Daily);
    retrieved.Schedule.Time.ShouldBe(new TimeOnly(10, 0));
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
    var task2 = await CreateTaskTemplateWithDependencies("Task 2", 3, Schedule.CreateDaily(new TimeOnly(9, 0)).Value);
    var task3 = await CreateTaskTemplateWithDependencies("Task 3", 1, Schedule.CreateDaily(new TimeOnly(10, 0)).Value);

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
    var task2 = await CreateTaskTemplateWithDependencies("Task 2", 3, Schedule.CreateDaily(new TimeOnly(9, 0)).Value);

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
