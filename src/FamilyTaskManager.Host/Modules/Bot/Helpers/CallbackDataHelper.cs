namespace FamilyTaskManager.Host.Modules.Bot.Helpers;

public static class CallbackDataHelper
{
  public static bool IsCallbackOf(this string[] callbackParts, Func<Guid, string> pattern, out Guid id)
  {
    id = Guid.Empty;
    return callbackParts.Length > 0
           && string.Join("_", [..callbackParts[..^1], id.EncodeToCallbackData()]) == pattern(id)
           && TryParseGuid(callbackParts.Last(), out id);
  }

  public static bool IsCallbackOf(this string[] callbackParts, Func<string> pattern) =>
    callbackParts.Length > 0 && string.Join("_", callbackParts) == pattern();

  public static string EncodeToCallbackData(this Guid guid) =>
    Convert.ToBase64String(guid.ToByteArray())
      .TrimEnd('=')
      .Replace('+', '-')
      .Replace('/', '.');

  public static bool TryParseGuid(string value, out Guid guid) =>
    Guid.TryParse(value, out guid) || TryDecodeGuid(value, out guid);

  private static bool TryDecodeGuid(string value, out Guid guid)
  {
    guid = Guid.Empty;

    if (string.IsNullOrWhiteSpace(value)) return false;

    var base64 = value
      .Replace('-', '+')
      .Replace('.', '/');

    switch (base64.Length % 4)
    {
      case 2:
        base64 += "==";
        break;
      case 3:
        base64 += "=";
        break;
      case 0:
        break;
      default:
        return false;
    }

    try
    {
      var bytes = Convert.FromBase64String(base64);
      guid = new(bytes);
      return true;
    }
    catch
    {
      return false;
    }
  }
}
