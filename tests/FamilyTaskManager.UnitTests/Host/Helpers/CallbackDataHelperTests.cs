using FamilyTaskManager.Host.Modules.Bot.Helpers;

namespace FamilyTaskManager.UnitTests.Host.Helpers;

public class CallbackDataHelperTests
{
  [Fact]
  public void IsCallbackOf_WithValidCallbackAndMatchingPattern_ReturnsTrue()
  {
    var testGuid = Guid.NewGuid();
    var encodedGuid = testGuid.EncodeToCallbackData();
    var callbackParts = new[] { "spot", "view", encodedGuid };
    Func<Guid, string> pattern = id => $"spot_view_{id.EncodeToCallbackData()}";

    var result = callbackParts.IsCallbackOf(pattern, out var parsedId);

    result.ShouldBeTrue();
    parsedId.ShouldBe(testGuid);
  }

  [Fact]
  public void IsCallbackOf_WithValidCallbackAndNonMatchingPattern_ReturnsFalse()
  {
    var testGuid = Guid.NewGuid();
    var encodedGuid = testGuid.EncodeToCallbackData();
    var callbackParts = new[] { "spot", "view", encodedGuid };
    Func<Guid, string> pattern = id => $"spot_delete_{id.EncodeToCallbackData()}";

    var result = callbackParts.IsCallbackOf(pattern, out var parsedId);

    result.ShouldBeFalse();
    parsedId.ShouldBe(Guid.Empty);
  }

  [Fact]
  public void IsCallbackOf_WithInvalidGuidInLastPart_ReturnsFalse()
  {
    var callbackParts = new[] { "spot", "view", "invalid-guid" };
    Func<Guid, string> pattern = id => $"spot_view_{id.EncodeToCallbackData()}";

    var result = callbackParts.IsCallbackOf(pattern, out var parsedId);

    result.ShouldBeFalse();
    parsedId.ShouldBe(Guid.Empty);
  }

  [Fact]
  public void IsCallbackOf_WithStandardGuidFormat_ReturnsTrue()
  {
    var testGuid = Guid.NewGuid();
    var callbackParts = new[] { "family", "select", testGuid.ToString() };
    Func<Guid, string> pattern = id => $"family_select_{id.EncodeToCallbackData()}";

    var result = callbackParts.IsCallbackOf(pattern, out var parsedId);

    result.ShouldBeTrue();
    parsedId.ShouldBe(testGuid);
  }

  [Fact]
  public void IsCallbackOf_WithEncodedGuidFormat_ReturnsTrue()
  {
    var testGuid = Guid.NewGuid();
    var encodedGuid = testGuid.EncodeToCallbackData();
    var callbackParts = new[] { "template", "view", encodedGuid };
    Func<Guid, string> pattern = id => $"template_view_{id.EncodeToCallbackData()}";

    var result = callbackParts.IsCallbackOf(pattern, out var parsedId);

    result.ShouldBeTrue();
    parsedId.ShouldBe(testGuid);
  }

  [Fact]
  public void IsCallbackOf_WithMultiplePartsBeforeGuid_ReturnsTrue()
  {
    var testGuid = Guid.NewGuid();
    var encodedGuid = testGuid.EncodeToCallbackData();
    var callbackParts = new[] { "conversation", "action", "subaction", encodedGuid };
    Func<Guid, string> pattern = id => $"conversation_action_subaction_{id.EncodeToCallbackData()}";

    var result = callbackParts.IsCallbackOf(pattern, out var parsedId);

    result.ShouldBeTrue();
    parsedId.ShouldBe(testGuid);
  }

  [Fact]
  public void IsCallbackOf_WithSinglePartAndGuid_ReturnsTrue()
  {
    var testGuid = Guid.NewGuid();
    var encodedGuid = testGuid.EncodeToCallbackData();
    var callbackParts = new[] { "action", encodedGuid };
    Func<Guid, string> pattern = id => $"action_{id.EncodeToCallbackData()}";

    var result = callbackParts.IsCallbackOf(pattern, out var parsedId);

    result.ShouldBeTrue();
    parsedId.ShouldBe(testGuid);
  }

  [Fact]
  public void IsCallbackOf_WithEmptyArray_ReturnsFalse()
  {
    var callbackParts = Array.Empty<string>();
    Func<Guid, string> pattern = id => $"action_{id.EncodeToCallbackData()}";

    var result = callbackParts.IsCallbackOf(pattern, out var parsedId);

    result.ShouldBeFalse();
    parsedId.ShouldBe(Guid.Empty);
  }

  [Fact]
  public void IsCallbackOf_WithSingleElementArray_ReturnsTrue()
  {
    var testGuid = Guid.NewGuid();
    var callbackParts = new[] { testGuid.ToString() };
    Func<Guid, string> pattern = id => $"{id.EncodeToCallbackData()}";

    var result = callbackParts.IsCallbackOf(pattern, out var parsedId);

    result.ShouldBeTrue();
    parsedId.ShouldBe(testGuid);
  }

  [Fact]
  public void IsCallbackOf_WithDifferentPrefixParts_ReturnsFalse()
  {
    var testGuid = Guid.NewGuid();
    var encodedGuid = testGuid.EncodeToCallbackData();
    var callbackParts = new[] { "spot", "delete", encodedGuid };
    Func<Guid, string> pattern = id => $"spot_view_{id.EncodeToCallbackData()}";

    var result = callbackParts.IsCallbackOf(pattern, out var parsedId);

    result.ShouldBeFalse();
    parsedId.ShouldBe(Guid.Empty);
  }

  [Fact]
  public void IsCallbackOf_WithDifferentNumberOfParts_ReturnsFalse()
  {
    var testGuid = Guid.NewGuid();
    var encodedGuid = testGuid.EncodeToCallbackData();
    var callbackParts = new[] { "spot", "view", "extra", encodedGuid };
    Func<Guid, string> pattern = id => $"spot_view_{id.EncodeToCallbackData()}";

    var result = callbackParts.IsCallbackOf(pattern, out var parsedId);

    result.ShouldBeFalse();
    parsedId.ShouldBe(Guid.Empty);
  }

  [Fact]
  public void IsCallbackOf_WithEmptyStringInLastPart_ReturnsFalse()
  {
    var callbackParts = new[] { "spot", "view", "" };
    Func<Guid, string> pattern = id => $"spot_view_{id.EncodeToCallbackData()}";

    var result = callbackParts.IsCallbackOf(pattern, out var parsedId);

    result.ShouldBeFalse();
    parsedId.ShouldBe(Guid.Empty);
  }

  [Fact]
  public void IsCallbackOf_WithWhitespaceInLastPart_ReturnsFalse()
  {
    var callbackParts = new[] { "spot", "view", "   " };
    Func<Guid, string> pattern = id => $"spot_view_{id.EncodeToCallbackData()}";

    var result = callbackParts.IsCallbackOf(pattern, out var parsedId);

    result.ShouldBeFalse();
    parsedId.ShouldBe(Guid.Empty);
  }

  [Fact]
  public void IsCallbackOf_WithSpecialCharactersInEncodedGuid_ReturnsTrue()
  {
    var testGuid = Guid.NewGuid();
    var encodedGuid = testGuid.EncodeToCallbackData();
    var callbackParts = new[] { "test", encodedGuid };
    Func<Guid, string> pattern = id => $"test_{id.EncodeToCallbackData()}";

    var result = callbackParts.IsCallbackOf(pattern, out var parsedId);

    result.ShouldBeTrue();
    parsedId.ShouldBe(testGuid);
    encodedGuid.ShouldNotContain("+");
    encodedGuid.ShouldNotContain("/");
    encodedGuid.ShouldNotContain("=");
  }

  [Theory]
  [InlineData("00000000-0000-0000-0000-000000000000")]
  [InlineData("ffffffff-ffff-ffff-ffff-ffffffffffff")]
  [InlineData("12345678-1234-1234-1234-123456789abc")]
  public void IsCallbackOf_WithVariousGuidValues_ReturnsTrue(string guidString)
  {
    var testGuid = Guid.Parse(guidString);
    var encodedGuid = testGuid.EncodeToCallbackData();
    var callbackParts = new[] { "action", encodedGuid };
    Func<Guid, string> pattern = id => $"action_{id.EncodeToCallbackData()}";

    var result = callbackParts.IsCallbackOf(pattern, out var parsedId);

    result.ShouldBeTrue();
    parsedId.ShouldBe(testGuid);
  }

  [Fact]
  public void IsCallbackOf_WithRealWorldSpotViewPattern_ReturnsTrue()
  {
    var spotId = Guid.NewGuid();
    var encodedSpotId = spotId.EncodeToCallbackData();
    var callbackParts = new[] { "SpotBrowsing", "view", encodedSpotId };
    Func<Guid, string> pattern = id => $"SpotBrowsing_view_{id.EncodeToCallbackData()}";

    var result = callbackParts.IsCallbackOf(pattern, out var parsedId);

    result.ShouldBeTrue();
    parsedId.ShouldBe(spotId);
  }

  [Fact]
  public void IsCallbackOf_WithRealWorldFamilySelectPattern_ReturnsTrue()
  {
    var familyId = Guid.NewGuid();
    var encodedFamilyId = familyId.EncodeToCallbackData();
    var callbackParts = new[] { "Family", "select", encodedFamilyId };
    Func<Guid, string> pattern = id => $"Family_select_{id.EncodeToCallbackData()}";

    var result = callbackParts.IsCallbackOf(pattern, out var parsedId);

    result.ShouldBeTrue();
    parsedId.ShouldBe(familyId);
  }
}
