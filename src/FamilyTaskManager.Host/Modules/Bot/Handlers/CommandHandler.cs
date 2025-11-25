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

public class CommandHandler(
  ISessionManager sessionManager,
  IMediator mediator,
  IUserRegistrationService userRegistrationService,
  IConversationRouter conversationRouter,
  FamilyCommandHandler familyCommandHandler,
  TasksCommandHandler tasksCommandHandler,
  PetCommandHandler petCommandHandler,
  StatsCommandHandler statsCommandHandler,
  TemplateCommandHandler templateCommandHandler)
  : ICommandHandler
{
  public async Task HandleCommandAsync(ITelegramBotClient botClient, Message message,
    CancellationToken cancellationToken)
  {
    var telegramId = message.From!.Id;
    var session = sessionManager.GetSession(telegramId);
    session.UpdateActivity();

    // Handle conversation state
    if (session.State != ConversationState.None)
    {
      // Handle universal commands
      var messageText = message.Text ?? string.Empty;
      if (messageText is "❌ Отменить" or "/cancel")
      {
        await conversationRouter.HandleCancelConversationAsync(
          botClient, message, session,
          () => SendMainMenuAsync(botClient, message.Chat.Id, cancellationToken),
          cancellationToken);
        return;
      }

      if (messageText == "⬅️ Назад")
      {
        await conversationRouter.HandleBackInConversationAsync(
          botClient, message, session,
          () => SendMainMenuAsync(botClient, message.Chat.Id, cancellationToken),
          cancellationToken);
        return;
      }

      await conversationRouter.HandleConversationAsync(botClient, message, session, cancellationToken);
      return;
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
      session.CurrentFamilyId = familiesResult.Value.First().Id;
      await SendMainMenuAsync(botClient, message.Chat.Id, cancellationToken);
    }
    else
    {
      // New user - offer to create family
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        "👋 Добро пожаловать в Семейный менеджер дел!\n\n" +
        BotConstants.Messages.NoFamiliesJoin,
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
        cancellationToken: cancellationToken);
      return;
    }

    await familyCommandHandler.HandleAsync(botClient, message, session, userResult.Value, cancellationToken);
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
        cancellationToken: cancellationToken);
      return;
    }

    await tasksCommandHandler.HandleAsync(botClient, message, session, userResult.Value, cancellationToken);
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
        cancellationToken: cancellationToken);
      return;
    }

    await petCommandHandler.HandleAsync(botClient, message, session, userResult.Value, cancellationToken);
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
        cancellationToken: cancellationToken);
      return;
    }

    await statsCommandHandler.HandleAsync(botClient, message, session, userResult.Value, cancellationToken);
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

    await templateCommandHandler.HandleAsync(botClient, message, session, userResult.Value, cancellationToken);
  }

  private async Task HandleHelpCommandAsync(
    ITelegramBotClient botClient,
    Message message,
    CancellationToken cancellationToken)
  {
    var helpText = @"📖 Справка по командам:

" + BotConstants.Help.Commands + @"

🔹 Используйте кнопки меню для быстрого доступа к функциям.";

    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      helpText,
      cancellationToken: cancellationToken);
  }

  private async Task HandleUnknownCommandAsync(
    ITelegramBotClient botClient,
    Message message,
    CancellationToken cancellationToken) =>
    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      BotConstants.Errors.UnknownCommand,
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
      "✅ Мои задачи" => HandleTasksCommandAsync(botClient, message, session, cancellationToken),
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
        cancellationToken: cancellationToken);
      return;
    }

    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      "⭐ Мои очки\n(В разработке)",
      cancellationToken: cancellationToken);
  }

  private async Task SendMainMenuAsync(
    ITelegramBotClient botClient,
    long chatId,
    CancellationToken cancellationToken)
  {
    var keyboard = new ReplyKeyboardMarkup(new[]
      {
        new KeyboardButton[] { "🏠 Семья", "✅ Мои задачи" }, new KeyboardButton[] { "🐾 Питомец", "⭐ Мои очки" },
        new KeyboardButton[] { "📊 Статистика" }
      })
      { ResizeKeyboard = true, IsPersistent = true };

    await botClient.SendTextMessageAsync(
      chatId,
      "🏠 Главное меню",
      replyMarkup: keyboard,
      cancellationToken: cancellationToken);
  }
}
