using FamilyTaskManager.Bot.Services;

namespace FamilyTaskManager.Bot;

public class Worker : BackgroundService
{
  private readonly ILogger<Worker> _logger;
  private readonly ITelegramBotService _botService;
  private readonly ISessionManager _sessionManager;

  public Worker(
    ILogger<Worker> logger,
    ITelegramBotService botService,
    ISessionManager sessionManager)
  {
    _logger = logger;
    _botService = botService;
    _sessionManager = sessionManager;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    _logger.LogInformation("Starting Telegram Bot Worker");

    // Start bot
    await _botService.StartAsync(stoppingToken);

    // Background task to clean up inactive sessions
    var cleanupTask = Task.Run(async () =>
    {
      while (!stoppingToken.IsCancellationRequested)
      {
        await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        _sessionManager.ClearInactiveSessions();
        _logger.LogInformation("Cleared inactive sessions");
      }
    }, stoppingToken);

    // Wait for cancellation
    await Task.Delay(Timeout.Infinite, stoppingToken);
  }

  public override async Task StopAsync(CancellationToken cancellationToken)
  {
    _logger.LogInformation("Stopping Telegram Bot Worker");
    await _botService.StopAsync(cancellationToken);
    await base.StopAsync(cancellationToken);
  }
}
