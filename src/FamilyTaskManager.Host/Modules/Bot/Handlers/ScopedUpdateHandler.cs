using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers;

/// <summary>
///   Wrapper that creates a scope for each update to resolve scoped handlers
/// </summary>
public class ScopedUpdateHandler : IUpdateHandler
{
  private readonly IServiceScopeFactory _scopeFactory;

  public ScopedUpdateHandler(IServiceScopeFactory scopeFactory)
  {
    _scopeFactory = scopeFactory;
  }

  public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
  {
    using var scope = _scopeFactory.CreateScope();
    var updateHandler = scope.ServiceProvider.GetRequiredService<IUpdateHandler>();
    await updateHandler.HandleUpdateAsync(botClient, update, cancellationToken);
  }

  public async Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception,
    CancellationToken cancellationToken)
  {
    using var scope = _scopeFactory.CreateScope();
    var updateHandler = scope.ServiceProvider.GetRequiredService<IUpdateHandler>();
    await updateHandler.HandlePollingErrorAsync(botClient, exception, cancellationToken);
  }
}
