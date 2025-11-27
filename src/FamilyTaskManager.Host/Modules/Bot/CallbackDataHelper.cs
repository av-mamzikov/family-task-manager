namespace FamilyTaskManager.Host.Modules.Bot;

internal static class CallbackDataHelper
{
  public static string EncodeGuid(Guid guid) =>
    Convert.ToBase64String(guid.ToByteArray())
      .TrimEnd('=')
      .Replace('+', '-')
      .Replace('/', '.');

  public static bool TryDecodeGuid(string value, out Guid guid)
  {
    guid = Guid.Empty;

    if (string.IsNullOrWhiteSpace(value))
    {
      return false;
    }

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
      guid = new Guid(bytes);
      return true;
    }
    catch
    {
      return false;
    }
  }
}
