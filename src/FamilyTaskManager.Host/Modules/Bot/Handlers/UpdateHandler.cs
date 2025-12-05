using FamilyTaskManager.Host.Modules.Bot.Constants;
using FamilyTaskManager.Host.Modules.Bot.Handlers.ConversationHandlers;
using FamilyTaskManager.Host.Modules.Bot.Helpers;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.Host.Modules.Bot.Services;
using FamilyTaskManager.UseCases.Families;
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

    var isMainMenuButton = messageText is "🏠 Семья" or "✅ Наши задачи" or "🧩 Споты" or "📊 Статистика";
    var isCommand = messageText.StartsWith('/');

    // Если есть активная conversation
    if (session.State != ConversationState.None)
    {
      // Если нажата кнопка главного меню или команда - отменяем conversation
      if (isMainMenuButton || isCommand)
        session.ClearState();
      else
      {
        var handler = GetConversationHandler(session.State);
        if (handler != null) await handler.HandleMessageAsync(botClient, message, session, cancellationToken);

        await sessionManager.SaveSessionAsync(session, cancellationToken);
        return;
      }
    }

    // Обработка команд и кнопок главного меню
    if (isCommand)
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
      await HandleKeyboardButtonAsync(botClient, message, session, cancellationToken);

    await sessionManager.SaveSessionAsync(session, cancellationToken);
  }

  private async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery,
    CancellationToken cancellationToken)
  {
    if (callbackQuery.Data is not { } data)
      return;

    var session = await sessionManager.GetSessionAsync(callbackQuery.From, cancellationToken);
    session.UpdateActivity();

    var chatId = callbackQuery.Message!.Chat.Id;
    var messageId = callbackQuery.Message.MessageId;
    var fromUser = callbackQuery.From;

    logger.LogInformation("Received callback from {ChatId}: {Data}", chatId, data);

    var parts = data.Split('_');
    var action = parts[0];

    var browsingState = GetBrowsingStateForAction(action);
    if (browsingState != ConversationState.None)
    {
      session.State = browsingState;
      session.Data = new();

      var conversationHandler = GetConversationHandler(browsingState);
      if (conversationHandler != null)
      {
        await conversationHandler.HandleCallbackAsync(botClient, chatId, messageId, parts, session, fromUser,
          cancellationToken);
        await sessionManager.SaveSessionAsync(session, cancellationToken);
        return;
      }
    }

    await botClient.SendTextMessageAsync(
      chatId,
      "❓ Неизвестное действие",
      cancellationToken: cancellationToken);

    await sessionManager.SaveSessionAsync(session, cancellationToken);
  }

  private IConversationHandler? GetConversationHandler(ConversationState state) =>
    state switch
    {
      ConversationState.FamilyCreation => serviceProvider.GetRequiredService<FamilyCreationHandler>(),
      ConversationState.SpotCreation => serviceProvider.GetRequiredService<SpotCreationHandler>(),
      ConversationState.TemplateForm => serviceProvider.GetRequiredService<TemplateFormHandler>(),
      ConversationState.TaskBrowsing => serviceProvider.GetRequiredService<TaskBrowsingHandler>(),
      ConversationState.TemplateBrowsing => serviceProvider.GetRequiredService<TemplateBrowsingHandler>(),
      ConversationState.SpotBrowsing => serviceProvider.GetRequiredService<SpotBrowsingHandler>(),
      ConversationState.Family => serviceProvider.GetRequiredService<FamilyHandler>(),
      ConversationState.FamilyMembers => serviceProvider.GetRequiredService<FamilyMembersHandler>(),
      ConversationState.StatsBrowsing => serviceProvider.GetRequiredService<StatsBrowsingHandler>(),
      _ => null
    };

  private ConversationState GetBrowsingStateForAction(string action) =>
    // points, timezone, schedule обрабатываются в активных Creation/Edit conversations
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
            BotMessages.Roles.GetRoleText(invitation.Member.Role)),
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
        + BotMessages.Messages.FamilyJoined(family.Name, BotMessages.Roles.GetRoleText(family.UserRole)));
    }
    else
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        BotMessages.Messages.WelcomeMessage +
        BotMessages.Messages.NoFamiliesJoin,
        parseMode: ParseMode.Markdown,
        replyMarkup: new InlineKeyboardMarkup([
          InlineKeyboardButton.WithCallbackData("➕ Создать семью", CallbackData.Family.Create)
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

  private async Task HandleUnknownCommandAsync(
    ITelegramBotClient botClient,
    Message message,
    CancellationToken cancellationToken) =>
    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      BotMessages.Errors.UnknownCommand,
      parseMode: ParseMode.Markdown,
      cancellationToken: cancellationToken);

  private async Task HandleKeyboardButtonAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    CancellationToken cancellationToken)
  {
    var text = message.Text!;

    await (text switch
    {
      "🏠 Семья" => HandleFamilyCommandAsync(botClient, message, session, cancellationToken),
      "✅ Наши задачи" => HandleTasksCommandAsync(botClient, message, session, cancellationToken),
      "🧩 Споты" => HandleSpotCommandAsync(botClient, message, session, cancellationToken),
      "📊 Статистика" => HandleStatsCommandAsync(botClient, message, session, cancellationToken),
      _ => HandleUnknownCommandAsync(botClient, message, cancellationToken)
    });
  }

  private async Task HandleFamilyCommandAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    CancellationToken cancellationToken)
  {
    session.State = ConversationState.Family;
    session.Data = new();

    var handler = serviceProvider.GetRequiredService<FamilyHandler>();
    await handler.ShowFamilyListAsync(botClient, message.Chat.Id, null, session.CurrentFamilyId, session,
      cancellationToken);
  }

  private async Task HandleTasksCommandAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    CancellationToken cancellationToken)
  {
    if (session.CurrentFamilyId == null)
    {
      await botClient.SendTextMessageAsync(message.Chat.Id, BotMessages.Errors.NoFamily,
        cancellationToken: cancellationToken);
      return;
    }

    session.State = ConversationState.TaskBrowsing;
    session.Data = new();

    // Просто отправляем сообщение, дальнейшая навигация через callbacks
    await botClient.SendTextMessageAsync(message.Chat.Id, "✅ Используйте кнопки для управления задачами",
      cancellationToken: cancellationToken);
  }

  private async Task HandleSpotCommandAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    CancellationToken cancellationToken)
  {
    if (session.CurrentFamilyId == null)
    {
      await botClient.SendTextMessageAsync(message.Chat.Id, BotMessages.Errors.NoFamily,
        cancellationToken: cancellationToken);
      return;
    }

    session.State = ConversationState.SpotBrowsing;
    session.Data = new();

    // Просто отправляем сообщение, дальнейшая навигация через callbacks
    await botClient.SendTextMessageAsync(message.Chat.Id, "🧩 Используйте кнопки для управления спотами",
      cancellationToken: cancellationToken);
  }

  private async Task HandleStatsCommandAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    CancellationToken cancellationToken)
  {
    if (session.CurrentFamilyId == null)
    {
      await botClient.SendTextMessageAsync(message.Chat.Id, BotMessages.Errors.NoFamily,
        cancellationToken: cancellationToken);
      return;
    }

    session.State = ConversationState.StatsBrowsing;
    session.Data = new();

    // Просто отправляем сообщение, дальнейшая навигация через callbacks
    await botClient.SendTextMessageAsync(message.Chat.Id, "📊 Используйте кнопки для просмотра статистики",
      cancellationToken: cancellationToken);
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
