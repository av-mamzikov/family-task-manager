using FamilyTaskManager.Host.Modules.Bot.Constants;
using FamilyTaskManager.Host.Modules.Bot.Models;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.CallbackHandlers;

public interface ICallbackRouter
{
  Task RouteCallbackAsync(
    ITelegramBotClient botClient,
    CallbackQuery callbackQuery,
    UserSession session,
    CancellationToken cancellationToken);
}

public class CallbackRouter(
  FamilyCallbackHandler familyCallbackHandler,
  SpotCallbackHandler SpotCallbackHandler,
  TaskCallbackHandler taskCallbackHandler,
  TemplateCallbackHandler templateCallbackHandler,
  TimezoneCallbackHandler timezoneCallbackHandler,
  ScheduleCallbackHandler scheduleCallbackHandler,
  PointsCallbackHandler pointsCallbackHandler)
  : ICallbackRouter
{
  public async Task RouteCallbackAsync(
    ITelegramBotClient botClient,
    CallbackQuery callbackQuery,
    UserSession session,
    CancellationToken cancellationToken)
  {
    var data = callbackQuery.Data!;
    var chatId = callbackQuery.Message!.Chat.Id;
    var messageId = callbackQuery.Message.MessageId;
    var fromUser = callbackQuery.From;

    // Parse callback data
    var parts = data.Split('_');
    var action = parts[0];

    await (action switch
    {
      CallbackData.Task.Entity => taskCallbackHandler.HandleTaskActionAsync(botClient, chatId, messageId, parts,
        session, fromUser,
        cancellationToken),
      CallbackData.Spot.Entity => SpotCallbackHandler.HandleSpotActionAsync(botClient, chatId, messageId, parts,
        session, fromUser,
        cancellationToken),
      CallbackData.Family.Entity => familyCallbackHandler.HandleFamilyActionAsync(botClient, chatId, messageId, parts,
        session,
        fromUser, cancellationToken),
      CallbackData.Timezone.Entity => timezoneCallbackHandler.HandleTimezoneSelectionAsync(botClient, chatId, messageId,
        parts, session,
        cancellationToken),
      CallbackData.Schedule.Entity => scheduleCallbackHandler.HandleScheduleCallbackAsync(botClient, chatId, messageId,
        parts, session,
        cancellationToken),
      CallbackData.Templates.Entity => templateCallbackHandler.HandleTemplateActionAsync(botClient, chatId, messageId,
        parts, session,
        fromUser, cancellationToken),
      CallbackData.Points.Entity => pointsCallbackHandler.HandlePointsSelectionAsync(botClient, chatId, messageId,
        parts, session,
        cancellationToken),
      _ => HandleUnknownCallbackAsync(botClient, chatId, cancellationToken)
    });
  }

  private static async Task HandleUnknownCallbackAsync(
    ITelegramBotClient botClient,
    long chatId,
    CancellationToken cancellationToken) =>
    await botClient.SendTextMessageAsync(
      chatId,
      "❓ Неизвестное действие",
      cancellationToken: cancellationToken);
}
