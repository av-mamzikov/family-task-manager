using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.SpotAggregate;
using FamilyTaskManager.Core.UserAggregate;

namespace FamilyTaskManager.UnitTests.Helpers;

public static class TestHelpers
{
  public static Spot CreateSpotWithFamily(string SpotName = "Test Spot", string timezone = "UTC")
  {
    var family = new Family("Test Family", timezone);
    var Spot = new Spot(family.Id, SpotType.Cat, SpotName);
    // Manually set navigation property for tests
    typeof(Spot).GetProperty("Family")!.SetValue(Spot, family);
    return Spot;
  }

  public static FamilyMember CreateMemberWithUser(string userName = "Test User")
  {
    var user = new User(123456789L, userName);
    var family = new Family("Test Family", "UTC");
    var member = family.AddMember(user, FamilyRole.Child);
    // Manually set navigation property for tests
    typeof(FamilyMember).GetProperty("User")!.SetValue(member, user);
    return member;
  }

  public static User CreateUser(string name = "Test User", long telegramId = 123456789L) =>
    new(telegramId, name);
}
