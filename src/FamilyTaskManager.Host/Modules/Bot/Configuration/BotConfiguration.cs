namespace FamilyTaskManager.Host.Modules.Bot.Configuration;

public class BotConfiguration
{
  private string _botUsername = string.Empty;
  public string BotToken { get; set; } = string.Empty;

  public string BotUsername
  {
    get => _botUsername.Trim().TrimStart('@');
    set => _botUsername = value ?? string.Empty;
  }
}
