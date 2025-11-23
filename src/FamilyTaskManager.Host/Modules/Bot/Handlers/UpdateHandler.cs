using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers;

public class UpdateHandler(
  ILogger<UpdateHandler> logger,
  IServiceProvider serviceProvider)
  : IUpdateHandler
{
  public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
  {
    try
    {
      var handler = update.Type switch
      {
        UpdateType.Message => HandleMessageAsync(botClient, update.Message!, cancellationToken),
        UpdateType.CallbackQuery => HandleCallbackQueryAsync(botClient, update.CallbackQuery!, cancellationToken),
        _ => Task.CompletedTask
      };

      await handler;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error handling update");
      await HandlePollingErrorAsync(botClient, ex, cancellationToken);
    }
  }

  public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception,
    CancellationToken cancellationToken)
  {
    logger.LogError(exception, "Telegram bot error");
    return Task.CompletedTask;
  }

  private async Task HandleMessageAsync(ITelegramBotClient botClient, Message message,
    CancellationToken cancellationToken)
  {
    var chatId = message.Chat.Id;

    // Handle location messages
    if (message.Location is not null)
    {
      logger.LogInformation("Received location from {ChatId}: Lat={Latitude}, Lon={Longitude}",
        chatId, message.Location.Latitude, message.Location.Longitude);
    }
    // Handle text messages
    else if (message.Text is not { } messageText)
    {
      return;
    }
    else
    {
      logger.LogInformation("Received message from {ChatId}: {MessageText}", chatId, messageText);
    }

    using var scope = serviceProvider.CreateScope();
    var commandHandler = scope.ServiceProvider.GetRequiredService<ICommandHandler>();

    await commandHandler.HandleCommandAsync(botClient, message, cancellationToken);
  }

  private async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery,
    CancellationToken cancellationToken)
  {
    if (callbackQuery.Data is not { } data)
    {
      return;
    }

    logger.LogInformation("Received callback from {ChatId}: {Data}", callbackQuery.Message?.Chat.Id, data);

    using var scope = serviceProvider.CreateScope();
    var callbackHandler = scope.ServiceProvider.GetRequiredService<ICallbackQueryHandler>();

    await callbackHandler.HandleCallbackAsync(botClient, callbackQuery, cancellationToken);
  }
}
