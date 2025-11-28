namespace FamilyTaskManager.Host.Modules.Bot.Services;

/// <summary>
///   Service to store runtime bot information
/// </summary>
public class BotInfoService
{
  public string? Username { get; private set; }

  public bool IsInitialized { get; private set; }

  public void SetBotInfo(string username)
  {
    Username = username;
    IsInitialized = true;
  }
}
