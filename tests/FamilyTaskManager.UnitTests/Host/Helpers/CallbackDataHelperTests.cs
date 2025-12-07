using FamilyTaskManager.Host.Modules.Bot.Helpers;
using FamilyTaskManager.Host.Modules.Bot.Models;

namespace FamilyTaskManager.UnitTests.Host.Helpers;

public class CallbackDataHelperTests
{
  [Fact]
  public void IsCallbackOf_WithMatchingPattern_ReturnsTrue()
  {
    EncodedGuid testGuid = Guid.NewGuid();
    var callbackParts = new string[] { "spot", "view", testGuid };
    Func<EncodedGuid, string> pattern = id => $"spot_view_{id}";

    var result = callbackParts.IsCallbackOf(pattern, out var parsedId);

    result.ShouldBeTrue();
    parsedId.ShouldBe(testGuid);
  }

  [Fact]
  public void IsCallbackOf_WithNonMatchingPattern_ReturnsFalse()
  {
    EncodedGuid testGuid = Guid.NewGuid();
    var callbackParts = new string[] { "spot", "view", testGuid };
    Func<EncodedGuid, string> pattern = id => $"spot_delete_{id}";

    var result = callbackParts.IsCallbackOf(pattern, out var parsedId);

    result.ShouldBeFalse();
    parsedId.ShouldBe((EncodedGuid)Guid.Empty);
  }

  [Fact]
  public void IsCallbackOf_WithInvalidGuid_ReturnsFalse()
  {
    var callbackParts = new[] { "spot", "view", "invalid-guid" };
    Func<EncodedGuid, string> pattern = id => $"spot_view_{id}";

    var result = callbackParts.IsCallbackOf(pattern, out var parsedId);

    result.ShouldBeFalse();
    parsedId.ShouldBe((EncodedGuid)Guid.Empty);
  }

  [Fact]
  public void IsCallbackOf_WithoutGuid_ReturnsTrue()
  {
    var callbackParts = new[] { "family", "list" };
    var pattern = () => "family_list";

    var result = callbackParts.IsCallbackOf(pattern);

    result.ShouldBeTrue();
  }

  [Fact]
  public void IsCallbackOf_WithoutGuid_WithNonMatchingPattern_ReturnsFalse()
  {
    var callbackParts = new[] { "family", "list" };
    var pattern = () => "family_create";

    var result = callbackParts.IsCallbackOf(pattern);

    result.ShouldBeFalse();
  }
}
