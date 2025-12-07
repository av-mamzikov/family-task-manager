using FamilyTaskManager.Host.Modules.Bot.Models;

namespace FamilyTaskManager.UnitTests.Host.Models;

public class EncodedGuidTests
{
  [Fact]
  public void RoundTrip_EncodingAndDecoding_PreservesGuid()
  {
    var originalGuid = Guid.NewGuid();

    EncodedGuid encoded = originalGuid;
    string encodedString = encoded;
    EncodedGuid decoded = encodedString;
    Guid resultGuid = decoded;

    resultGuid.ShouldBe(originalGuid);
    encodedString.ShouldNotContain("+");
    encodedString.ShouldNotContain("/");
    encodedString.ShouldNotContain("=");
  }

  [Fact]
  public void TryParse_WithStandardGuidString_ReturnsTrue()
  {
    var guid = Guid.NewGuid();

    var result = EncodedGuid.TryParse(guid.ToString(), out var parsed);

    result.ShouldBeTrue();
    ((Guid)parsed).ShouldBe(guid);
  }

  [Fact]
  public void TryParse_WithInvalidString_ReturnsFalse()
  {
    var result = EncodedGuid.TryParse("invalid-guid", out var parsed);

    result.ShouldBeFalse();
    ((Guid)parsed).ShouldBe(Guid.Empty);
  }

  [Fact]
  public void Parse_WithInvalidString_ThrowsFormatException() =>
    Should.Throw<FormatException>(() => EncodedGuid.Parse("invalid-guid"));
}
