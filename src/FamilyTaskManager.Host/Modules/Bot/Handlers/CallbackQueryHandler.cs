using FamilyTaskManager.Host.Modules.Bot.Handlers.CallbackHandlers;
using FamilyTaskManager.Host.Modules.Bot.Models;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers;

public class CallbackQueryHandler(
  ILogger<CallbackQueryHandler> logger,
  ICallbackRouter callbackRouter)
  : ICallbackQueryHandler
{
  public async Task HandleCallbackAsync(
    ITelegramBotClient botClient,
    CallbackQuery callbackQuery,
    UserSession session,
    CancellationToken cancellationToken)
  {
    var data = callbackQuery.Data!;
    var chatId = callbackQuery.Message!.Chat.Id;

    try
    {
      // Answer callback query to remove loading state
      await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);

      // Route to appropriate handler
      await callbackRouter.RouteCallbackAsync(botClient, callbackQuery, session, cancellationToken);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error handling callback: {Data}", data);
      await botClient.SendTextMessageAsync(
        chatId,
        "❌ Произошла ошибка. Попробуйте снова.",
        cancellationToken: cancellationToken);
    }
  }
}
