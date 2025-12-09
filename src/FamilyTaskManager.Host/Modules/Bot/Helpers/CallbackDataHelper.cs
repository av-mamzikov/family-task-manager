using FamilyTaskManager.Host.Modules.Bot.Models;

namespace FamilyTaskManager.Host.Modules.Bot.Helpers;

public static class CallbackDataHelper
{
  public static bool IsCallbackOf(this string[] callbackParts, Func<EncodedGuid, string> pattern, out EncodedGuid id)
  {
    id = Guid.Empty;
    return callbackParts.Length > 0
           && string.Join("_", [..callbackParts[..^1], id.ToString()]) == pattern(id)
           && EncodedGuid.TryParse(callbackParts.Last(), out id);
  }

  public static bool IsCallbackOf(this string[] callbackParts, Func<EncodedGuid, string, string> pattern,
    out EncodedGuid id, out string stringParam)
  {
    id = Guid.Empty;
    stringParam = string.Empty;
    return callbackParts.Length >= 2
           && string.Join("_", [..callbackParts[..^2], id.ToString(), stringParam]) == pattern(id, stringParam)
           && EncodedGuid.TryParse(callbackParts[^2], out id)
           && (stringParam = callbackParts[^1]) != null;
  }

  public static bool IsCallbackOf(this string[] callbackParts, Func<EncodedGuid, EncodedGuid, string> pattern,
    out EncodedGuid firstId, out EncodedGuid secondId)
  {
    firstId = Guid.Empty;
    secondId = Guid.Empty;

    if (callbackParts.Length < 2) return false;

    if (string.Join("_", [..callbackParts[..^2], firstId.ToString(), secondId.ToString()]) !=
        pattern(firstId, secondId))
      return false;

    return EncodedGuid.TryParse(callbackParts[^2], out firstId) &&
           EncodedGuid.TryParse(callbackParts[^1], out secondId);
  }

  public static bool IsCallbackOf(this string[] callbackParts, Func<string, string> pattern, out string stringParam)
  {
    stringParam = string.Empty;
    if (callbackParts.Length == 0) return false;

    stringParam = callbackParts.Last();
    return string.Join("_", [..callbackParts[..^1], stringParam]) == pattern(stringParam);
  }

  public static bool IsCallbackOf(this string[] callbackParts, Func<string> pattern) =>
    callbackParts.Length > 0 && string.Join("_", callbackParts) == pattern();

  public static bool IsCallbackOf(this string[] callbackParts, Func<int, string> pattern, out int intParam)
  {
    intParam = 0;
    return callbackParts.Length > 0
           && int.TryParse(callbackParts.Last(), out intParam)
           && string.Join("_", [..callbackParts[..^1], intParam.ToString()]) == pattern(intParam);
  }
}
