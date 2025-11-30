using FamilyTaskManager.Core.UserAggregate;

namespace FamilyTaskManager.IntegrationTests.Data;

public class UserRepositoryTests : RepositoryTestsBase<User>
{
  protected override User CreateTestEntity(string uniqueSuffix = "") =>
    new(123456789 + long.Parse(uniqueSuffix == "" ? "0" : uniqueSuffix), $"Test User{uniqueSuffix}");

  protected override User CreateSecondTestEntity(string uniqueSuffix = "") =>
    new(987654321 + long.Parse(uniqueSuffix == "" ? "0" : uniqueSuffix), $"Another User{uniqueSuffix}");

  protected override void ModifyEntity(User entity) => entity.UpdateName("Updated Name");

  protected override void AssertEntityWasModified(User entity) => entity.Name.ShouldBe("Updated Name");

  [Fact]
  public async Task User_ShouldHaveCorrectTelegramId()
  {
    // Arrange
    var user = new User(555555555, "Telegram User");

    // Act
    await Repository.AddAsync(user);
    await DbContext.SaveChangesAsync();

    // Assert
    var retrieved = await Repository.GetByIdAsync(user.Id);
    retrieved.ShouldNotBeNull();
    retrieved.TelegramId.ShouldBe(555555555);
    retrieved.Name.ShouldBe("Telegram User");
  }
}
