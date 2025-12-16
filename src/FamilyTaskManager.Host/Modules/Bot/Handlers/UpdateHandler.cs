using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Host.Modules.Bot.Constants;
using FamilyTaskManager.Host.Modules.Bot.Handlers.ConversationHandlers;
using FamilyTaskManager.Host.Modules.Bot.Helpers;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.Host.Modules.Bot.Services;
using FamilyTaskManager.UseCases.Features.FamilyManagement.Commands;
using FamilyTaskManager.UseCases.Features.FamilyManagement.Queries;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

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

    var messageText = message.Text ?? string.Empty;
    logger.LogInformation("Received message from {ChatId}: {MessageText}", chatId, messageText);

    // Обработка команд и кнопок главного меню
    if (messageText.StartsWith('/'))
    {
      var command = messageText.Split(' ')[0].ToLower();
      var args = messageText.Split(' ').Skip(1).ToArray();

      await (command switch
      {
        "/start" => HandleStartCommandAsync(botClient, message, args, session, cancellationToken),
        "/help" => HandleHelpCommandAsync(botClient, message, cancellationToken),
        _ => HandleUnknownCommandAsync(botClient, message, cancellationToken)
      });
    }
    else
    {
      if (await HandleMainMenuButtonAsync(botClient, message, session, cancellationToken))
        return;

      // Если есть активная conversation
      if (session.State != ConversationState.None)
      {
        var handler = GetConversationHandler(session.State);
        if (handler != null) await handler.HandleMessageAsync(botClient, message, session, cancellationToken);

        await sessionManager.SaveSessionAsync(session, cancellationToken);
        return;
      }
    }

    await sessionManager.SaveSessionAsync(session, cancellationToken);
  }

  private async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery,
    CancellationToken cancellationToken)
  {
    if (callbackQuery.Data is not { } data)
      return;
    var session = await sessionManager.GetSessionAsync(callbackQuery.From, cancellationToken);
    session.UpdateActivity();

    var message = callbackQuery.Message!;
    var chatId = message.Chat.Id;
    var fromUser = message.From!;
    await HandleCallback(botClient, session, data, chatId, callbackQuery.Message, fromUser, cancellationToken);

    await sessionManager.SaveSessionAsync(session, cancellationToken);
  }

  private async Task HandleCallback(ITelegramBotClient botClient, UserSession session, string callbackData,
    long chatId, Message? message, User? fromUser, CancellationToken cancellationToken)
  {
    var parts = callbackData.Split('_');
    var conversation = parts[0];

    logger.LogInformation("Received callback from {ChatId}: {Data}", chatId, callbackData);

    var targetState = GetBrowsingStateForAction(conversation);
    session.State = targetState;
    var handler = GetConversationHandler(targetState);
    if (handler != null)
    {
      await handler.HandleCallbackAsync(botClient, chatId, message, parts, session, fromUser!,
        cancellationToken);
      await sessionManager.SaveSessionAsync(session, cancellationToken);
      return;
    }

    logger.LogError("Handler for {Conversation} not found", conversation);

    await botClient.SendTextMessageAsync(
      chatId,
      "❓ Неизвестное действие",
      cancellationToken: cancellationToken);
  }

  private IConversationHandler? GetConversationHandler(ConversationState state) =>
    state switch
    {
      ConversationState.FamilyCreation => serviceProvider.GetRequiredService<FamilyCreationHandler>(),
      ConversationState.SpotCreation => serviceProvider.GetRequiredService<SpotCreationHandler>(),
      ConversationState.TemplateForm => serviceProvider.GetRequiredService<TemplateFormHandler>(),
      ConversationState.Tasks => serviceProvider.GetRequiredService<TaskBrowsingHandler>(),
      ConversationState.Templates => serviceProvider.GetRequiredService<TemplateBrowsingHandler>(),
      ConversationState.Spots => serviceProvider.GetRequiredService<SpotBrowsingHandler>(),
      ConversationState.Family => serviceProvider.GetRequiredService<FamilyBrowsingHandler>(),
      ConversationState.Families => serviceProvider.GetRequiredService<FamilyMembersBrousingHandler>(),
      ConversationState.Stats => serviceProvider.GetRequiredService<StatsBrowsingHandler>(),
      _ => null
    };

  private ConversationState GetBrowsingStateForAction(string action) =>
    Enum.TryParse(action, out ConversationState newState)
      ? newState
      : ConversationState.None;

  private async Task HandleStartCommandAsync(
    ITelegramBotClient botClient,
    Message message,
    string[] args,
    UserSession session,
    CancellationToken cancellationToken)
  {
    var mediator = serviceProvider.GetRequiredService<IMediator>();

    // Проверка на invite code
    if (args.Length > 0 && args[0].StartsWith("invite_"))
    {
      var code = args[0].Replace("invite_", "");
      var joinCommand = new JoinByInviteCodeCommand(session.UserId, code);
      var invitationResult = await mediator.Send(joinCommand, cancellationToken);

      if (invitationResult.IsSuccess)
      {
        var invitation = invitationResult.Value;
        session.CurrentFamilyId = invitation.Family.Id;
        await botClient.SendTextMessageAsync(
          message.Chat.Id,
          "🎉 *Добро пожаловать в семью!*\n\n" +
          BotMessages.Messages.FamilyJoined(invitation.Family.Name,
            RoleDisplay.GetRoleCaption(invitation.Member.Role)),
          parseMode: ParseMode.Markdown,
          cancellationToken: cancellationToken);
        await SendMainMenuAsync(botClient, message.Chat.Id, cancellationToken);
      }
      else
      {
        var errorMessage = invitationResult.Errors.FirstOrDefault() ?? BotMessages.Errors.UnknownError;
        await botClient.SendTextMessageAsync(
          message.Chat.Id,
          $"❌ Не удалось присоединиться к семье:\n{errorMessage}",
          parseMode: ParseMode.Markdown,
          cancellationToken: cancellationToken);
      }

      return;
    }

    // Получение семей пользователя
    var getFamiliesQuery = new GetUserFamiliesQuery(session.UserId);
    var familiesResult = await mediator.Send(getFamiliesQuery, cancellationToken);

    if (familiesResult.IsSuccess && familiesResult.Value.Any())
    {
      var family = familiesResult.Value.First();
      session.CurrentFamilyId = family.Id;
      await SendMainMenuAsync(botClient, message.Chat.Id, cancellationToken,
        BotMessages.Messages.WelcomeMessage
        + BotMessages.Messages.FamilyJoined(family.Name, RoleDisplay.GetRoleCaption(family.UserRole)));
    }
    else
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        BotMessages.Messages.WelcomeMessage +
        BotMessages.Messages.NoFamiliesJoin,
        parseMode: ParseMode.Markdown,
        replyMarkup: new InlineKeyboardMarkup([
          InlineKeyboardButton.WithCallbackData("➕ Создать семью", CallbackData.Family.Create())
        ]),
        cancellationToken: cancellationToken);
  }

  private async Task HandleHelpCommandAsync(
    ITelegramBotClient botClient,
    Message message,
    CancellationToken cancellationToken) =>
    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      BotMessages.Help.Commands,
      parseMode: ParseMode.Markdown,
      cancellationToken: cancellationToken);

  private async Task<bool> HandleUnknownCommandAsync(
    ITelegramBotClient botClient,
    Message message,
    CancellationToken cancellationToken)
  {
    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      BotMessages.Errors.UnknownCommand,
      parseMode: ParseMode.Markdown,
      cancellationToken: cancellationToken);
    return false;
  }

  private async Task<bool> HandleMainMenuButtonAsync(ITelegramBotClient botClient, Message message, UserSession session,
    CancellationToken cancellationToken)
  {
    var text = message.Text!;

    var callBackData = text switch
    {
      "🏠 Семья" => CallbackData.Family.List(),
      "✅ Наши задачи" => CallbackData.TaskBrowsing.List(),
      "🧩 Споты" => CallbackData.SpotBrowsing.List(),
      "📊 Статистика" => CallbackData.Stats.List(),
      _ => null
    };
    if (callBackData != null)
    {
      await HandleCallback(botClient, session, callBackData, message.Chat.Id, null, message.From, cancellationToken);
      return true;
    }

    return false;
  }

  private async Task SendMainMenuAsync(
    ITelegramBotClient botClient,
    long chatId,
    CancellationToken cancellationToken,
    string welcomeMessage = "") =>
    await botClient.SendTextMessageAsync(
      chatId,
      welcomeMessage + "🏠 Главное меню",
      parseMode: ParseMode.Markdown,
      replyMarkup: MainMenuHelper.GetMainMenuKeyboard(),
      cancellationToken: cancellationToken);
}
