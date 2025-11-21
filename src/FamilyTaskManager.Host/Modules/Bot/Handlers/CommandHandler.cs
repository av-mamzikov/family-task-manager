using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using FamilyTaskManager.Host.Modules.Bot.Services;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.Host.Modules.Bot.Handlers.Commands;
using FamilyTaskManager.UseCases.Users;
using FamilyTaskManager.UseCases.Families;
using FamilyTaskManager.UseCases.Pets;
using FamilyTaskManager.Core.PetAggregate;
using Mediator;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers;

public class CommandHandler : ICommandHandler
{
  private readonly ILogger<CommandHandler> _logger;
  private readonly ISessionManager _sessionManager;
  private readonly IMediator _mediator;
  private readonly FamilyCommandHandler _familyCommandHandler;
  private readonly TasksCommandHandler _tasksCommandHandler;
  private readonly PetCommandHandler _petCommandHandler;
  private readonly StatsCommandHandler _statsCommandHandler;

  public CommandHandler(
    ILogger<CommandHandler> logger,
    ISessionManager sessionManager,
    IMediator mediator,
    FamilyCommandHandler familyCommandHandler,
    TasksCommandHandler tasksCommandHandler,
    PetCommandHandler petCommandHandler,
    StatsCommandHandler statsCommandHandler)
  {
    _logger = logger;
    _sessionManager = sessionManager;
    _mediator = mediator;
    _familyCommandHandler = familyCommandHandler;
    _tasksCommandHandler = tasksCommandHandler;
    _petCommandHandler = petCommandHandler;
    _statsCommandHandler = statsCommandHandler;
  }

  public async Task HandleCommandAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
  {
    var telegramId = message.From!.Id;
    var session = _sessionManager.GetSession(telegramId);
    session.UpdateActivity();

    // Handle conversation state
    if (session.State != ConversationState.None)
    {
      await HandleConversationAsync(botClient, message, session, cancellationToken);
      return;
    }

    // Handle commands
    var messageText = message.Text!;
    if (messageText.StartsWith('/'))
    {
      var command = messageText.Split(' ')[0].ToLower();
      var args = messageText.Split(' ').Skip(1).ToArray();

      await (command switch
      {
        "/start" => HandleStartCommandAsync(botClient, message, args, session, cancellationToken),
        "/family" => HandleFamilyCommandAsync(botClient, message, session, cancellationToken),
        "/tasks" => HandleTasksCommandAsync(botClient, message, session, cancellationToken),
        "/pet" => HandlePetCommandAsync(botClient, message, session, cancellationToken),
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
    var userName = $"{message.From.FirstName} {message.From.LastName}".Trim();
    if (string.IsNullOrEmpty(userName))
      userName = message.From.Username ?? "User";

    // Register or update user
    var registerCommand = new RegisterUserCommand(telegramId, userName);
    var result = await _mediator.Send(registerCommand, cancellationToken);

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
    var familiesResult = await _mediator.Send(getFamiliesQuery, cancellationToken);

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
        "У вас пока нет семей. Создайте свою первую семью или присоединитесь к существующей.",
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
    // TODO: Implement invite handling
    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      "🔗 Обработка приглашения...\n(В разработке)",
      cancellationToken: cancellationToken);
  }

  private async Task HandleFamilyCommandAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    CancellationToken cancellationToken)
  {
    // Get user ID
    var registerCommand = new RegisterUserCommand(message.From!.Id, message.From.FirstName ?? "User");
    var userResult = await _mediator.Send(registerCommand, cancellationToken);
    
    if (!userResult.IsSuccess)
    {
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        "❌ Ошибка. Попробуйте /start",
        cancellationToken: cancellationToken);
      return;
    }

    await _familyCommandHandler.HandleAsync(botClient, message, session, userResult.Value, cancellationToken);
  }

  private async Task HandleTasksCommandAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    CancellationToken cancellationToken)
  {
    var registerCommand = new RegisterUserCommand(message.From!.Id, message.From.FirstName ?? "User");
    var userResult = await _mediator.Send(registerCommand, cancellationToken);
    
    if (!userResult.IsSuccess)
    {
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        "❌ Ошибка. Попробуйте /start",
        cancellationToken: cancellationToken);
      return;
    }

    await _tasksCommandHandler.HandleAsync(botClient, message, session, userResult.Value, cancellationToken);
  }

  private async Task HandlePetCommandAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    CancellationToken cancellationToken)
  {
    var registerCommand = new RegisterUserCommand(message.From!.Id, message.From.FirstName ?? "User");
    var userResult = await _mediator.Send(registerCommand, cancellationToken);
    
    if (!userResult.IsSuccess)
    {
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        "❌ Ошибка. Попробуйте /start",
        cancellationToken: cancellationToken);
      return;
    }

    await _petCommandHandler.HandleAsync(botClient, message, session, userResult.Value, cancellationToken);
  }

  private async Task HandleStatsCommandAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    CancellationToken cancellationToken)
  {
    var registerCommand = new RegisterUserCommand(message.From!.Id, message.From.FirstName ?? "User");
    var userResult = await _mediator.Send(registerCommand, cancellationToken);
    
    if (!userResult.IsSuccess)
    {
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        "❌ Ошибка. Попробуйте /start",
        cancellationToken: cancellationToken);
      return;
    }

    await _statsCommandHandler.HandleAsync(botClient, message, session, userResult.Value, cancellationToken);
  }

  private async Task HandleHelpCommandAsync(
    ITelegramBotClient botClient,
    Message message,
    CancellationToken cancellationToken)
  {
    var helpText = @"📖 Справка по командам:

/start - Начать работу с ботом
/family - Управление семьями
/tasks - Просмотр активных задач
/pet - Просмотр питомцев
/stats - Статистика и лидерборд
/help - Эта справка

🔹 Используйте кнопки меню для быстрого доступа к функциям.";

    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      helpText,
      cancellationToken: cancellationToken);
  }

  private async Task HandleUnknownCommandAsync(
    ITelegramBotClient botClient,
    Message message,
    CancellationToken cancellationToken)
  {
    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      "❓ Неизвестная команда. Используйте /help для списка команд.",
      cancellationToken: cancellationToken);
  }

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

  private async Task HandleConversationAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    CancellationToken cancellationToken)
  {
    var text = message.Text!;

    switch (session.State)
    {
      case ConversationState.AwaitingFamilyName:
        await HandleFamilyNameInputAsync(botClient, message, session, text, cancellationToken);
        break;
      
      case ConversationState.AwaitingPetName:
        await HandlePetNameInputAsync(botClient, message, session, text, cancellationToken);
        break;

      // Add more conversation handlers as needed
      default:
        session.ClearState();
        await botClient.SendTextMessageAsync(
          message.Chat.Id,
          "❌ Произошла ошибка. Попробуйте снова.",
          cancellationToken: cancellationToken);
        break;
    }
  }

  private async Task HandleFamilyNameInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string familyName,
    CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(familyName) || familyName.Length < 3)
    {
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        "❌ Название семьи должно содержать минимум 3 символа. Попробуйте снова:",
        cancellationToken: cancellationToken);
      return;
    }

    // Get userId from session data
    if (!session.Data.TryGetValue("userId", out var userIdObj) || userIdObj is not Guid userId)
    {
      session.ClearState();
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        "❌ Ошибка. Попробуйте создать семью заново.",
        cancellationToken: cancellationToken);
      return;
    }

    // Create family
    var createFamilyCommand = new CreateFamilyCommand(userId, familyName);
    var result = await _mediator.Send(createFamilyCommand, cancellationToken);

    if (!result.IsSuccess)
    {
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        $"❌ Ошибка создания семьи: {result.Errors.FirstOrDefault()}",
        cancellationToken: cancellationToken);
      session.ClearState();
      return;
    }

    session.CurrentFamilyId = result.Value;
    session.ClearState();

    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      $"✅ Семья \"{familyName}\" успешно создана!\n\n" +
      "Теперь вы можете добавить питомца и создать задачи.",
      cancellationToken: cancellationToken);

    await SendMainMenuAsync(botClient, message.Chat.Id, cancellationToken);
  }

  private async Task HandlePetNameInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string petName,
    CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(petName) || petName.Length < 2 || petName.Length > 50)
    {
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        "❌ Имя питомца должно содержать от 2 до 50 символов. Попробуйте снова:",
        cancellationToken: cancellationToken);
      return;
    }

    // Get data from session
    if (!session.Data.TryGetValue("petType", out var petTypeObj) || petTypeObj is not string petTypeStr ||
        !session.Data.TryGetValue("familyId", out var familyIdObj) || familyIdObj is not Guid familyId)
    {
      session.ClearState();
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        "❌ Ошибка. Попробуйте создать питомца заново.",
        cancellationToken: cancellationToken);
      return;
    }

    // Parse pet type
    if (!Enum.TryParse<PetType>(petTypeStr, true, out var petType))
    {
      session.ClearState();
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        "❌ Ошибка типа питомца. Попробуйте создать питомца заново.",
        cancellationToken: cancellationToken);
      return;
    }

    // Create pet
    var createPetCommand = new CreatePetCommand(familyId, petType, petName);
    var result = await _mediator.Send(createPetCommand, cancellationToken);

    if (!result.IsSuccess)
    {
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        $"❌ Ошибка создания питомца: {result.Errors.FirstOrDefault()}",
        cancellationToken: cancellationToken);
      session.ClearState();
      return;
    }

    session.ClearState();

    var petEmoji = petType switch
    {
      PetType.Cat => "🐱",
      PetType.Dog => "🐶",
      PetType.Hamster => "🐹",
      _ => "🐾"
    };

    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      $"✅ Питомец {petEmoji} \"{petName}\" успешно создан!\n\n" +
      "Теперь вы можете создавать задачи для ухода за питомцем.",
      cancellationToken: cancellationToken);
  }

  private async Task SendMainMenuAsync(
    ITelegramBotClient botClient,
    long chatId,
    CancellationToken cancellationToken)
  {
    var keyboard = new ReplyKeyboardMarkup(new[]
    {
      new KeyboardButton[] { "🏠 Семья", "✅ Мои задачи" },
      new KeyboardButton[] { "🐾 Питомец", "⭐ Мои очки" },
      new KeyboardButton[] { "📊 Статистика" }
    })
    {
      ResizeKeyboard = true,
      IsPersistent = true
    };

    await botClient.SendTextMessageAsync(
      chatId,
      "🏠 Главное меню",
      replyMarkup: keyboard,
      cancellationToken: cancellationToken);
  }
}
