using FamilyTaskManager.Host.Modules.Bot.Services;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers;

public class UpdateHandler(
  ILogger<UpdateHandler> logger,
  IServiceProvider serviceProvider,
  ISessionManager sessionManager)
  : IUpdateHandler
{
  public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
  {
    try
    {
      switch (update.Type)
      {
        case UpdateType.Message:
          await HandleMessageAsync(botClient, update.Message!, cancellationToken);
          break;
        case UpdateType.CallbackQuery:
          await HandleCallbackQueryAsync(botClient, update.CallbackQuery!, cancellationToken);
          break;
        default:
          return;
      }
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
    if (message.From == null)
    {
      logger.LogError("Message from unknown user is unsupported");
      return;
    }

    var session = await sessionManager.GetSessionAsync(message.From, cancellationToken);
    session.UpdateActivity();

    // Handle location messages
    if (message.Location is not null)
      logger.LogInformation("Received location from {ChatId}: Lat={Latitude}, Lon={Longitude}",
        chatId, message.Location.Latitude, message.Location.Longitude);
    // Handle text messages
    else if (message.Text is not { } messageText)
      return;
    else
      logger.LogInformation("Received message from {ChatId}: {MessageText}", chatId, messageText);


    //using var scope = serviceProvider.CreateScope();
    var commandHandler = serviceProvider.GetRequiredService<IMessageHandler>();

    await commandHandler.HandleCommandAsync(botClient, message, session, cancellationToken);

    await sessionManager.SaveSessionAsync(session, cancellationToken);
  }

  private async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery,
    CancellationToken cancellationToken)
  {
    if (callbackQuery.Data is not { } data)
      return;
    var session = await sessionManager.GetSessionAsync(callbackQuery.From, cancellationToken);
    session.UpdateActivity();

    logger.LogInformation("Received callback from {ChatId}: {Data}", callbackQuery.Message?.Chat.Id, data);

    using var scope = serviceProvider.CreateScope();
    var callbackHandler = scope.ServiceProvider.GetRequiredService<ICallbackQueryHandler>();

    await callbackHandler.HandleCallbackAsync(botClient, callbackQuery, session, cancellationToken);

    await sessionManager.SaveSessionAsync(session, cancellationToken);
  }
}
