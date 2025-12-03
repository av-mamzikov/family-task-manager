using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.PetAggregate;
using FamilyTaskManager.Core.UserAggregate;

namespace FamilyTaskManager.UnitTests.Helpers;

public static class TestHelpers
{
  public static Pet CreatePetWithFamily(string petName = "Test Pet", string timezone = "UTC")
  {
    var family = new Family("Test Family", timezone);
    var pet = new Pet(family.Id, PetType.Cat, petName);
    // Manually set navigation property for tests
    typeof(Pet).GetProperty("Family")!.SetValue(pet, family);
    return pet;
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
