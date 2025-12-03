using FamilyTaskManager.Host.Modules.Bot.Handlers.Commands;
using FamilyTaskManager.Host.Modules.Bot.Handlers.ConversationHandlers;
using FamilyTaskManager.Host.Modules.Bot.Helpers;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.UseCases.Families;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers;

public interface IMessageHandler
{
  Task HandleCommandAsync(ITelegramBotClient botClient, Message message, UserSession session,
    CancellationToken cancellationToken);
}

public class MessageHandler(
  IMediator mediator,
  IConversationRouter conversationRouter,
  IServiceProvider serviceProvider)
  : IMessageHandler
{
  public async Task HandleCommandAsync(ITelegramBotClient botClient, Message message, UserSession session,
    CancellationToken cancellationToken)
  {
    var messageText = message.Text ?? string.Empty;

    // Check if user pressed main menu button while in conversation
    var isMainMenuButton =
      messageText is "🏠 Семья" or "✅ Наши задачи" or "🐾 Питомец" or "📊 Статистика";
    var isCommand = messageText.StartsWith('/');

    // Handle conversation state
    if (session.State != ConversationState.None)
    {
      // If user pressed main menu button or command, clear conversation state
      if (isMainMenuButton || isCommand)
      {
        await botClient.SendTextMessageAsync(
          message.Chat.Id,
          "❌ Предыдущее действие отменено.",
          parseMode: ParseMode.Markdown,
          replyMarkup: new ReplyKeyboardRemove(),
          cancellationToken: cancellationToken);
        // Continue to handle the button/command
        session.ClearState();
      }
      // Handle universal conversation commands
      else if (messageText is "❌ Отменить" or "/cancel")
      {
        await conversationRouter.HandleCancelConversationAsync(
          botClient, message, session,
          () => SendMainMenuAsync(botClient, message.Chat.Id, cancellationToken),
          cancellationToken);
        return;
      }
      else if (messageText == "⬅️ Назад")
      {
        await conversationRouter.HandleBackInConversationAsync(
          botClient, message, session,
          () => SendMainMenuAsync(botClient, message.Chat.Id, cancellationToken),
          cancellationToken);
        return;
      }
      else
      {
        await conversationRouter.HandleConversationAsync(botClient, message, session, cancellationToken);
        return;
      }
    }

    // Handle commands
    var commandText = message.Text!;
    if (commandText.StartsWith('/'))
    {
      var command = commandText.Split(' ')[0].ToLower();
      var args = commandText.Split(' ').Skip(1).ToArray();

      await (command switch
      {
        "/start" => HandleStartCommandAsync(botClient, message, args, session, cancellationToken),
        "/help" => HandleHelpCommandAsync(botClient, message, cancellationToken),
        _ => HandleUnknownCommandAsync(botClient, message, cancellationToken)
      });
    }
    else
      // Handle persistent keyboard buttons
      await HandleKeyboardButtonAsync(botClient, message, session, cancellationToken);
  }

  private async Task HandleStartCommandAsync(
    ITelegramBotClient botClient,
    Message message,
    string[] args,
    UserSession session,
    CancellationToken cancellationToken)
  {
    // Check for invite code
    if (args.Length > 0 && args[0].StartsWith("invite_"))
    {
      await HandleInviteAsync(botClient, message, session, args[0], cancellationToken);
      return;
    }

    // Get user families
    var getFamiliesQuery = new GetUserFamiliesQuery(session.UserId);
    var familiesResult = await mediator.Send(getFamiliesQuery, cancellationToken);

    if (familiesResult.IsSuccess && familiesResult.Value.Any())
    {
      // User has families
      var family = familiesResult.Value.First();
      session.CurrentFamilyId = family.Id;
      await SendMainMenuAsync(botClient, message.Chat.Id, cancellationToken,
        BotConstants.Messages.WelcomeMessage
        + BotConstants.Messages.FamilyJoined(family.Name, BotConstants.Roles.GetRoleText(family.UserRole)));
    }
    else
      // New user - offer to create family
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        BotConstants.Messages.WelcomeMessage +
        BotConstants.Messages.NoFamiliesJoin,
        parseMode: ParseMode.Markdown,
        replyMarkup: new InlineKeyboardMarkup(new[]
        {
          InlineKeyboardButton.WithCallbackData("➕ Создать семью", "create_family")
        }),
        cancellationToken: cancellationToken);
  }

  private async Task HandleInviteAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string inviteCode,
    CancellationToken cancellationToken)
  {
    var code = inviteCode.Replace("invite_", "");
    var joinCommand = new JoinByInviteCodeCommand(session.UserId, code);
    var invitationResult = await mediator.Send(joinCommand, cancellationToken);

    if (invitationResult.IsSuccess)
    {
      var invitation = invitationResult.Value;
      session.CurrentFamilyId = invitation.Family.Id;
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        "🎉 *Добро пожаловать в семью!*\n\n" +
        BotConstants.Messages.FamilyJoined(invitation.Family.Name,
          BotConstants.Roles.GetRoleText(invitation.Member.Role)),
        parseMode: ParseMode.Markdown,
        cancellationToken: cancellationToken);
      await SendMainMenuAsync(botClient, message.Chat.Id, cancellationToken);
    }
    else
    {
      var errorMessage = invitationResult.Errors.FirstOrDefault() ?? BotConstants.Errors.UnknownError;
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        $"❌ Не удалось присоединиться к семье:\n{errorMessage}",
        parseMode: ParseMode.Markdown,
        cancellationToken: cancellationToken);
    }
  }

  private async Task HandleFamilyCommandAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    CancellationToken cancellationToken)
  {
    var handler = serviceProvider.GetRequiredService<FamilyCommandHandler>();
    await handler.HandleAsync(botClient, message, session, session.UserId, cancellationToken);
  }

  private async Task HandleTasksCommandAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    CancellationToken cancellationToken)
  {
    var handler = serviceProvider.GetRequiredService<TasksCommandHandler>();
    await handler.HandleAsync(botClient, message, session, session.UserId, cancellationToken);
  }

  private async Task HandlePetCommandAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    CancellationToken cancellationToken)
  {
    var handler = serviceProvider.GetRequiredService<PetCommandHandler>();
    await handler.HandleAsync(botClient, message, session, session.UserId, cancellationToken);
  }

  private async Task HandleStatsCommandAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    CancellationToken cancellationToken)
  {
    var handler = serviceProvider.GetRequiredService<StatsCommandHandler>();
    await handler.HandleAsync(botClient, message, session, session.UserId, cancellationToken);
  }

  private async Task HandleHelpCommandAsync(
    ITelegramBotClient botClient,
    Message message,
    CancellationToken cancellationToken) =>
    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      BotConstants.Help.Commands,
      parseMode: ParseMode.Markdown,
      cancellationToken: cancellationToken);

  private async Task HandleUnknownCommandAsync(
    ITelegramBotClient botClient,
    Message message,
    CancellationToken cancellationToken) =>
    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      BotConstants.Errors.UnknownCommand,
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
      "🐾 Питомец" => HandlePetCommandAsync(botClient, message, session, cancellationToken),
      "📊 Статистика" => HandleStatsCommandAsync(botClient, message, session, cancellationToken),
      _ => HandleUnknownCommandAsync(botClient, message, cancellationToken)
    });
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
