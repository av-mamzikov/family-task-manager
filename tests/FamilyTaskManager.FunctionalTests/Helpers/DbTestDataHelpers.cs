using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.UserAggregate;
using FamilyTaskManager.Infrastructure.Data;

namespace FamilyTaskManager.FunctionalTests.Helpers;

public static class DbTestDataHelpers
{
  public static async Task<(Family family, User admin)> CreateFamilyWithAdminInDbAsync(
    CustomWebApplicationFactory<Program> factory,
    string familyName = "Test Family",
    long? telegramId = null,
    string userName = "Test User",
    string timezone = "Europe/Moscow",
    bool leaderboardEnabled = true)
  {
    using var scope = factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    var actualTelegramId = telegramId ?? TestDataBuilder.GenerateTelegramId();
    var user = new User(actualTelegramId, userName);
    var family = new Family(familyName, timezone, leaderboardEnabled);
    family.AddMember(user, FamilyRole.Admin);

    db.Users.Add(user);
    db.Families.Add(family);
    await db.SaveChangesAsync();

    return (family, user);
  }
}
