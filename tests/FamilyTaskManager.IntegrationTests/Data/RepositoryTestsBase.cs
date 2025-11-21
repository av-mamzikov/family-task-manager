namespace FamilyTaskManager.IntegrationTests.Data;

/// <summary>
/// Базовый класс с типовыми тестами для всех репозиториев.
/// Наследники должны реализовать абстрактные методы для создания тестовых сущностей.
/// </summary>
public abstract class RepositoryTestsBase<TEntity> : BaseRepositoryTestFixture
  where TEntity : EntityBase<TEntity, Guid>, IAggregateRoot
{
  protected IRepository<TEntity> Repository => GetRepository<TEntity>();

  /// <summary>
  /// Создает новую тестовую сущность для добавления
  /// </summary>
  protected abstract TEntity CreateTestEntity(string uniqueSuffix = "");

  /// <summary>
  /// Создает вторую тестовую сущность (для тестов со множественными сущностями)
  /// </summary>
  protected abstract TEntity CreateSecondTestEntity(string uniqueSuffix = "");

  /// <summary>
  /// Модифицирует существующую сущность для теста обновления
  /// </summary>
  protected abstract void ModifyEntity(TEntity entity);

  /// <summary>
  /// Проверяет, что сущность была корректно модифицирована
  /// </summary>
  protected abstract void AssertEntityWasModified(TEntity entity);

  [Fact]
  public virtual async Task AddAsync_ShouldPersistEntityToDatabase()
  {
    // Arrange
    var entity = CreateTestEntity();

    // Act
    await Repository.AddAsync(entity);
    await DbContext.SaveChangesAsync();

    // Assert
    var retrieved = await Repository.GetByIdAsync(entity.Id);
    retrieved.ShouldNotBeNull();
    retrieved.Id.ShouldBe(entity.Id);
  }

  [Fact]
  public virtual async Task UpdateAsync_ShouldModifyExistingEntity()
  {
    // Arrange
    var entity = CreateTestEntity();
    await Repository.AddAsync(entity);
    await DbContext.SaveChangesAsync();

    // Act
    ModifyEntity(entity);
    await Repository.UpdateAsync(entity);
    await DbContext.SaveChangesAsync();

    // Assert
    var retrieved = await Repository.GetByIdAsync(entity.Id);
    retrieved.ShouldNotBeNull();
    AssertEntityWasModified(retrieved);
  }

  [Fact]
  public virtual async Task DeleteAsync_ShouldRemoveEntityFromDatabase()
  {
    // Arrange
    var entity = CreateTestEntity();
    await Repository.AddAsync(entity);
    await DbContext.SaveChangesAsync();

    // Act
    await Repository.DeleteAsync(entity);
    await DbContext.SaveChangesAsync();

    // Assert
    var retrieved = await Repository.GetByIdAsync(entity.Id);
    retrieved.ShouldBeNull();
  }

  [Fact]
  public virtual async Task GetByIdAsync_WithNonExistentId_ShouldReturnNull()
  {
    // Arrange
    var nonExistentId = Guid.NewGuid();

    // Act
    var result = await Repository.GetByIdAsync(nonExistentId);

    // Assert
    result.ShouldBeNull();
  }

  [Fact]
  public virtual async Task ListAsync_ShouldReturnAllEntities()
  {
    // Arrange
    var entity1 = CreateTestEntity("1");
    var entity2 = CreateSecondTestEntity("2");

    await Repository.AddRangeAsync([entity1, entity2]);
    await DbContext.SaveChangesAsync();

    // Act
    var entities = await Repository.ListAsync();

    // Assert
    entities.Count.ShouldBe(2);
    entities.ShouldContain(e => e.Id == entity1.Id);
    entities.ShouldContain(e => e.Id == entity2.Id);
  }

  [Fact]
  public virtual async Task CountAsync_ShouldReturnCorrectCount()
  {
    // Arrange
    var entity1 = CreateTestEntity("1");
    var entity2 = CreateSecondTestEntity("2");

    await Repository.AddRangeAsync([entity1, entity2]);
    await DbContext.SaveChangesAsync();

    // Act
    var count = await Repository.CountAsync();

    // Assert
    count.ShouldBe(2);
  }

  [Fact]
  public virtual async Task AnyAsync_WithExistingData_ShouldReturnTrue()
  {
    // Arrange
    var entity = CreateTestEntity();
    await Repository.AddAsync(entity);
    await DbContext.SaveChangesAsync();

    // Act
    var exists = await Repository.AnyAsync();

    // Assert
    exists.ShouldBeTrue();
  }

  [Fact]
  public virtual async Task AnyAsync_WithEmptyDatabase_ShouldReturnFalse()
  {
    // Act
    var exists = await Repository.AnyAsync();

    // Assert
    exists.ShouldBeFalse();
  }

  [Fact]
  public virtual async Task AddRangeAsync_ShouldPersistMultipleEntities()
  {
    // Arrange
    var entity1 = CreateTestEntity("1");
    var entity2 = CreateSecondTestEntity("2");

    // Act
    await Repository.AddRangeAsync([entity1, entity2]);
    await DbContext.SaveChangesAsync();

    // Assert
    var allEntities = await Repository.ListAsync();
    allEntities.Count.ShouldBe(2);
    allEntities.ShouldContain(e => e.Id == entity1.Id);
    allEntities.ShouldContain(e => e.Id == entity2.Id);
  }
}
