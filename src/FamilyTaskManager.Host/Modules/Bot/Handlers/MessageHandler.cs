using FamilyTaskManager.Host.Modules.Bot.Handlers.Commands;
using FamilyTaskManager.Host.Modules.Bot.Handlers.ConversationHandlers;
using FamilyTaskManager.Host.Modules.Bot.Helpers;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.Host.Modules.Bot.Services;
using FamilyTaskManager.UseCases.Families;
using FamilyTaskManager.UseCases.Users;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers;

public interface IMessageHandler
{
  Task HandleCommandAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken);
}

public class MessageHandler(
  ISessionManager sessionManager,
  IMediator mediator,
  IUserRegistrationService userRegistrationService,
  IConversationRouter conversationRouter,
  IServiceProvider serviceProvider)
  : IMessageHandler
{
  public async Task HandleCommandAsync(ITelegramBotClient botClient, Message message,
    CancellationToken cancellationToken)
  {
    var telegramId = message.From!.Id;
    var session = sessionManager.GetSession(telegramId);
    session.UpdateActivity();

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
        session.ClearState();
        await botClient.SendTextMessageAsync(
          message.Chat.Id,
          "❌ Предыдущее действие отменено.",
          parseMode: ParseMode.Markdown,
          replyMarkup: new ReplyKeyboardRemove(),
          cancellationToken: cancellationToken);
        // Continue to handle the button/command
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
        "/family" => HandleFamilyCommandAsync(botClient, message, session, cancellationToken),
        "/tasks" => HandleTasksCommandAsync(botClient, message, session, cancellationToken),
        "/pet" => HandlePetCommandAsync(botClient, message, session, cancellationToken),
        "/templates" => HandleTemplatesCommandAsync(botClient, message, session, cancellationToken),
        "/stats" => HandleStatsCommandAsync(botClient, message, session, cancellationToken),
        "/help" => HandleHelpCommandAsync(botClient, message, cancellationToken),
        _ => HandleUnknownCommandAsync(botClient, message, cancellationToken)
      });
    }
    else
    {
      // Handle persistent keyboard buttons
      await HandleKeyboardButtonAsync(botClient, message, session, cancellationToken);
    }
  }

  private async Task HandleStartCommandAsync(
    ITelegramBotClient botClient,
    Message message,
    string[] args,
    UserSession session,
    CancellationToken cancellationToken)
  {
    var telegramId = message.From!.Id;
    var userName = message.From.GetDisplayName();

    // Register or update user
    var registerCommand = new RegisterUserCommand(telegramId, userName);
    var result = await mediator.Send(registerCommand, cancellationToken);

    if (!result.IsSuccess)
    {
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        "❌ Ошибка регистрации. Попробуйте позже.",
        parseMode: ParseMode.Markdown,
        cancellationToken: cancellationToken);
      return;
    }

    var userId = result.Value;

    // Check for invite code
    if (args.Length > 0 && args[0].StartsWith("invite_"))
    {
      await HandleInviteAsync(botClient, message, userId, args[0], cancellationToken);
      return;
    }

    // Get user families
    var getFamiliesQuery = new GetUserFamiliesQuery(userId);
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
    {
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
  }

  private async Task HandleInviteAsync(
    ITelegramBotClient botClient,
    Message message,
    Guid userId,
    string inviteCode,
    CancellationToken cancellationToken)
  {
    // Extract code from "invite_CODE" format
    var code = inviteCode.Replace("invite_", "");

    // Join family by invite code
    var joinCommand = new JoinByInviteCodeCommand(userId, code);
    var result = await mediator.Send(joinCommand, cancellationToken);

    if (!result.IsSuccess)
    {
      var errorMessage = result.Errors.FirstOrDefault() ?? BotConstants.Errors.UnknownError;
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        $"❌ Не удалось присоединиться к семье:\n{errorMessage}",
        parseMode: ParseMode.Markdown,
        cancellationToken: cancellationToken);
      return;
    }

    // Get updated family list
    var getFamiliesQuery = new GetUserFamiliesQuery(userId);
    var familiesResult = await mediator.Send(getFamiliesQuery, cancellationToken);

    if (familiesResult.IsSuccess && familiesResult.Value.Any())
    {
      var newFamily = familiesResult.Value.First();
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        $"🎉 *Добро пожаловать в семью!*\n\n" +
        BotConstants.Messages.FamilyJoined(newFamily.Name, BotConstants.Roles.GetRoleText(newFamily.UserRole)),
        parseMode: ParseMode.Markdown,
        cancellationToken: cancellationToken);

      // Show main menu
      await SendMainMenuAsync(botClient, message.Chat.Id, cancellationToken);
    }
    else
    {
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        "✅ Вы присоединились к семье!",
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
    var userResult = await userRegistrationService.GetOrRegisterUserAsync(message.From!, cancellationToken);

    if (!userResult.IsSuccess)
    {
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        "❌ Ошибка. Попробуйте /start",
        parseMode: ParseMode.Markdown,
        cancellationToken: cancellationToken);
      return;
    }

    var handler = serviceProvider.GetRequiredService<FamilyCommandHandler>();
    await handler.HandleAsync(botClient, message, session, userResult.Value, cancellationToken);
  }

  private async Task HandleTasksCommandAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    CancellationToken cancellationToken)
  {
    var userResult = await userRegistrationService.GetOrRegisterUserAsync(message.From!, cancellationToken);

    if (!userResult.IsSuccess)
    {
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        "❌ Ошибка. Попробуйте /start",
        parseMode: ParseMode.Markdown,
        cancellationToken: cancellationToken);
      return;
    }

    var handler = serviceProvider.GetRequiredService<TasksCommandHandler>();
    await handler.HandleAsync(botClient, message, session, userResult.Value, cancellationToken);
  }

  private async Task HandlePetCommandAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    CancellationToken cancellationToken)
  {
    var userResult = await userRegistrationService.GetOrRegisterUserAsync(message.From!, cancellationToken);

    if (!userResult.IsSuccess)
    {
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        "❌ Ошибка. Попробуйте /start",
        parseMode: ParseMode.Markdown,
        cancellationToken: cancellationToken);
      return;
    }

    var handler = serviceProvider.GetRequiredService<PetCommandHandler>();
    await handler.HandleAsync(botClient, message, session, userResult.Value, cancellationToken);
  }

  private async Task HandleStatsCommandAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    CancellationToken cancellationToken)
  {
    var userResult = await userRegistrationService.GetOrRegisterUserAsync(message.From!, cancellationToken);

    if (!userResult.IsSuccess)
    {
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        "❌ Ошибка. Попробуйте /start",
        parseMode: ParseMode.Markdown,
        cancellationToken: cancellationToken);
      return;
    }

    var handler = serviceProvider.GetRequiredService<StatsCommandHandler>();
    await handler.HandleAsync(botClient, message, session, userResult.Value, cancellationToken);
  }

  private async Task HandleTemplatesCommandAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    CancellationToken cancellationToken)
  {
    var userResult = await userRegistrationService.GetOrRegisterUserAsync(message.From!, cancellationToken);

    if (!userResult.IsSuccess)
    {
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        "❌ Ошибка. Попробуйте /start",
        cancellationToken: cancellationToken);
      return;
    }

    var handler = serviceProvider.GetRequiredService<TemplateCommandHandler>();
    await handler.HandleAsync(botClient, message, session, userResult.Value, cancellationToken);
  }

  private async Task HandleHelpCommandAsync(
    ITelegramBotClient botClient,
    Message message,
    CancellationToken cancellationToken)
  {
    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      BotConstants.Help.Commands,
      parseMode: ParseMode.Markdown,
      cancellationToken: cancellationToken);
  }

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
      "⭐ Мои очки" => HandleMyPointsAsync(botClient, message, session, cancellationToken),
      "📊 Статистика" => HandleStatsCommandAsync(botClient, message, session, cancellationToken),
      _ => HandleUnknownCommandAsync(botClient, message, cancellationToken)
    });
  }

  private async Task HandleMyPointsAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    CancellationToken cancellationToken)
  {
    if (session.CurrentFamilyId == null)
    {
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        "❌ Сначала выберите активную семью через /family",
        parseMode: ParseMode.Markdown,
        cancellationToken: cancellationToken);
      return;
    }

    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      "⭐ Мои очки\n(В разработке)",
      parseMode: ParseMode.Markdown,
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
