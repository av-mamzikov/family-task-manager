namespace FamilyTaskManager.Host.Modules.Bot.Models;

public enum StartPayloadType
{
  None,
  Campaign,
  Invite
}

public sealed class StartPayload
{
  private StartPayload(StartPayloadType type, string? value)
  {
    Type = type;
    Value = value;
  }

  public StartPayloadType Type { get; }
  public string? Value { get; }

  public static StartPayload None { get; } = new(StartPayloadType.None, null);

  public static StartPayload Parse(string? messageText)
  {
    if (string.IsNullOrWhiteSpace(messageText))
      return None;

    if (!messageText.StartsWith('/'))
      return None;

    var parts = messageText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length <= 1)
      return None;

    var command = parts[0].ToLowerInvariant();
    if (command != "/start")
      return None;

    var payload = parts[1];
    var segments = payload.Split('_', 2, StringSplitOptions.RemoveEmptyEntries);
    if (segments.Length < 2)
      return None;

    var type = segments[0].ToLowerInvariant();
    var value = segments[1];

    if (string.IsNullOrWhiteSpace(value))
      return None;

    return type switch
    {
      "campaign" => new(StartPayloadType.Campaign, value),
      "invite" => new(StartPayloadType.Invite, value),
      _ => None
    };
  }
}
