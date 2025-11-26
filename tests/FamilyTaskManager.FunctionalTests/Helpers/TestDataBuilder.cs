using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.UserAggregate;

namespace FamilyTaskManager.FunctionalTests.Helpers;

/// <summary>
///   Builder for creating test data objects
/// </summary>
public static class TestDataBuilder
{
  /// <summary>
  ///   Create a test user with default values
  /// </summary>
  public static User CreateUser(
    long telegramId = 123456789,
    string name = "Test User") =>
    new(telegramId, name);

  /// <summary>
  ///   Create a test family with default values
  /// </summary>
  public static Family CreateFamily(
    string name = "Test Family",
    string timezone = "UTC",
    bool leaderboardEnabled = true) =>
    new(name, timezone, leaderboardEnabled);

  /// <summary>
  ///   Create a family with admin member
  /// </summary>
  public static (Family family, User user) CreateFamilyWithAdmin(
    string familyName = "Test Family",
    long telegramId = 123456789,
    string userName = "Test User")
  {
    var user = CreateUser(telegramId, userName);
    var family = CreateFamily(familyName);
    family.AddMember(user.Id, FamilyRole.Admin);
    return (family, user);
  }

  /// <summary>
  ///   Create a family with multiple members
  /// </summary>
  public static (Family family, List<User> users) CreateFamilyWithMembers(
    string familyName = "Test Family",
    int memberCount = 3)
  {
    var family = CreateFamily(familyName);
    var users = new List<User>();

    for (var i = 0; i < memberCount; i++)
    {
      var user = CreateUser(123456789 + i, $"User {i}");
      users.Add(user);

      var role = i == 0 ? FamilyRole.Admin : i % 2 == 0 ? FamilyRole.Adult : FamilyRole.Child;
      family.AddMember(user.Id, role);
    }

    return (family, users);
  }

  /// <summary>
  ///   Generate random telegram ID for testing
  /// </summary>
  public static long GenerateTelegramId() => Random.Shared.NextInt64(100000000, 999999999);

  /// <summary>
  ///   Generate random chat ID for testing
  /// </summary>
  public static long GenerateChatId() => Random.Shared.NextInt64(100000000, 999999999);
}
