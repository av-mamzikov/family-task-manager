namespace FamilyTaskManager.Host.Modules.Bot.Models;

public readonly record struct EncodedGuid(Guid Value)
{
  public readonly string Encoded = EncodeToCallbackData(Value);
  public static implicit operator string(EncodedGuid encoded) => encoded.Encoded;
  public static implicit operator EncodedGuid(Guid guid) => new(guid);
  public static implicit operator Guid(EncodedGuid encoded) => encoded.Value;
  public static implicit operator EncodedGuid(string value) => Parse(value);

  public override string ToString() => Encoded;

  private static string EncodeToCallbackData(Guid guid) =>
    Convert.ToBase64String(guid.ToByteArray())
      .TrimEnd('=')
      .Replace('+', '-')
      .Replace('/', '.');

  public static Guid Parse(string value) => TryParse(value, out var guid)
    ? guid
    : throw new FormatException($"Invalid encoded GUID format: {value}");

  public static bool TryParse(string value, out EncodedGuid encodedGuid)
  {
    if (!Guid.TryParse(value, out var guid)) return TryDecodeGuid(value, out encodedGuid);
    encodedGuid = guid;
    return true;
  }

  private static bool TryDecodeGuid(string value, out EncodedGuid guid)
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
      guid = new Guid(bytes);
      return true;
    }
    catch
    {
      return false;
    }
  }
}
