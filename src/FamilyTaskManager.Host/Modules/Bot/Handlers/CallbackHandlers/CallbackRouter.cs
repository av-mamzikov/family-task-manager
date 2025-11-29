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
  PetCallbackHandler petCallbackHandler,
  TaskCallbackHandler taskCallbackHandler,
  TemplateCallbackHandler templateCallbackHandler,
  TimezoneCallbackHandler timezoneCallbackHandler,
  ScheduleCallbackHandler scheduleCallbackHandler)
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
      "create" => HandleCreateActionAsync(botClient, chatId, messageId, parts, session, fromUser, cancellationToken),
      "select" => HandleSelectActionAsync(botClient, chatId, messageId, parts, session, cancellationToken),
      "task" => taskCallbackHandler.HandleTaskActionAsync(botClient, chatId, messageId, parts, session, fromUser,
        cancellationToken),
      "taskpet" => taskCallbackHandler.HandleTaskPetSelectionAsync(botClient, chatId, messageId, parts, session,
        cancellationToken),
      "pet" => petCallbackHandler.HandlePetActionAsync(botClient, chatId, messageId, parts, session, fromUser,
        cancellationToken),
      "family" => familyCallbackHandler.HandleFamilyActionAsync(botClient, chatId, messageId, parts, session,
        fromUser, cancellationToken),
      "invite" => familyCallbackHandler.HandleInviteActionAsync(botClient, chatId, messageId, parts, session,
        fromUser, cancellationToken),
      "timezone" => timezoneCallbackHandler.HandleTimezoneSelectionAsync(botClient, chatId, messageId, parts, session,
        cancellationToken),
      "schedule" => scheduleCallbackHandler.HandleScheduleCallbackAsync(botClient, chatId, messageId, parts, session,
        cancellationToken),
      "tpl" => templateCallbackHandler.HandleTemplateActionAsync(botClient, chatId, messageId, parts, session,
        fromUser, cancellationToken),
      "confirm" => HandleConfirmActionAsync(botClient, chatId, messageId, parts, session, fromUser, cancellationToken),
      "cancel" => HandleCancelActionAsync(botClient, chatId, messageId, parts, session, cancellationToken),
      _ => HandleUnknownCallbackAsync(botClient, chatId, cancellationToken)
    });
  }

  private async Task HandleCreateActionAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    string[] parts,
    UserSession session,
    User fromUser,
    CancellationToken cancellationToken)
  {
    if (parts.Length < 2)
    {
      return;
    }

    var entityType = parts[1];

    switch (entityType)
    {
      case "family":
        await familyCallbackHandler.StartCreateFamilyAsync(botClient, chatId, messageId, session, fromUser,
          cancellationToken);
        break;

      case "pet":
        await petCallbackHandler.StartCreatePetAsync(botClient, chatId, messageId, session, cancellationToken);
        break;

      case "task":
        await taskCallbackHandler.StartCreateTaskAsync(botClient, chatId, messageId, session, cancellationToken);
        break;
    }
  }

  private async Task HandleSelectActionAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    string[] parts,
    UserSession session,
    CancellationToken cancellationToken)
  {
    if (parts.Length < 3)
    {
      return;
    }

    var selectType = parts[1];
    var value = parts[2];

    switch (selectType)
    {
      case "pettype":
        await petCallbackHandler.HandlePetTypeSelectionAsync(botClient, chatId, messageId, value, session,
          cancellationToken);
        break;

      case "family":
        await familyCallbackHandler.HandleFamilySelectionAsync(botClient, chatId, messageId, value, session,
          cancellationToken);
        break;

      case "tasktype":
        await taskCallbackHandler.HandleTaskTypeSelectionAsync(botClient, chatId, messageId, value, session,
          cancellationToken);
        break;
    }
  }

  private async Task HandleConfirmActionAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    string[] parts,
    UserSession session,
    User fromUser,
    CancellationToken cancellationToken)
  {
    if (parts.Length < 3)
    {
      return;
    }

    var confirmType = parts[1];
    var familyIdStr = parts[2];

    if (confirmType == "delete" && Guid.TryParse(familyIdStr, out var familyId))
    {
      await familyCallbackHandler.HandleConfirmDeleteFamilyAsync(botClient, chatId, messageId, familyId, session,
        fromUser, cancellationToken);
    }
  }

  private static async Task HandleCancelActionAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    string[] parts,
    UserSession session,
    CancellationToken cancellationToken)
  {
    if (parts.Length < 2)
    {
      return;
    }

    var cancelType = parts[1];

    if (cancelType == "delete")
    {
      await botClient.EditMessageTextAsync(
        chatId,
        messageId,
        "❌ Удаление семьи отменено",
        cancellationToken: cancellationToken);
    }
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
