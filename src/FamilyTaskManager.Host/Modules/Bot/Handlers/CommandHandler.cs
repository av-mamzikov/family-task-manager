using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.Interfaces;
using FamilyTaskManager.Core.PetAggregate;
using FamilyTaskManager.Host.Modules.Bot.Handlers.Commands;
using FamilyTaskManager.Host.Modules.Bot.Helpers;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.Host.Modules.Bot.Services;
using FamilyTaskManager.UseCases.Families;
using FamilyTaskManager.UseCases.Pets;
using FamilyTaskManager.UseCases.Tasks;
using FamilyTaskManager.UseCases.TaskTemplates;
using FamilyTaskManager.UseCases.Users;
using GeoTimeZone;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers;

public class CommandHandler(
  ILogger<CommandHandler> logger,
  ISessionManager sessionManager,
  IMediator mediator,
  FamilyCommandHandler familyCommandHandler,
  TasksCommandHandler tasksCommandHandler,
  PetCommandHandler petCommandHandler,
  StatsCommandHandler statsCommandHandler,
  TemplateCommandHandler templateCommandHandler,
  ITimeZoneService timeZoneService)
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

  private static InlineKeyboardMarkup GetRussianTimeZoneKeyboard()
  {
    return new InlineKeyboardMarkup(new[]
    {
      new[] { InlineKeyboardButton.WithCallbackData("🇷🇺 Калининград", "timezone_Europe/Kaliningrad") },
      new[] { InlineKeyboardButton.WithCallbackData("🇷🇺 Москва", "timezone_Europe/Moscow") },
      new[] { InlineKeyboardButton.WithCallbackData("🇷🇺 Самара", "timezone_Europe/Samara") },
      new[] { InlineKeyboardButton.WithCallbackData("🇷🇺 Екатеринбург", "timezone_Asia/Yekaterinburg") },
      new[] { InlineKeyboardButton.WithCallbackData("🇷🇺 Омск", "timezone_Asia/Omsk") },
      new[] { InlineKeyboardButton.WithCallbackData("🇷🇺 Красноярск", "timezone_Asia/Krasnoyarsk") },
      new[] { InlineKeyboardButton.WithCallbackData("🇷🇺 Иркутск", "timezone_Asia/Irkutsk") },
      new[] { InlineKeyboardButton.WithCallbackData("🇷🇺 Якутск", "timezone_Asia/Yakutsk") },
      new[] { InlineKeyboardButton.WithCallbackData("🇷🇺 Владивосток", "timezone_Asia/Vladivostok") },
      new[] { InlineKeyboardButton.WithCallbackData("🇷🇺 Магадан", "timezone_Asia/Magadan") },
      new[] { InlineKeyboardButton.WithCallbackData("🇷🇺 Камчатка", "timezone_Asia/Kamchatka") },
      new[] { InlineKeyboardButton.WithCallbackData("📍 Определить по геолокации", "timezone_detect") },
      new[] { InlineKeyboardButton.WithCallbackData("⏭️ Пропустить (UTC)", "timezone_UTC") }
    });
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

  private string GetRoleText(FamilyRole role)
  {
    return role switch
    {
      FamilyRole.Admin => "👑 Администратор",
      FamilyRole.Adult => "👤 Взрослый",
      FamilyRole.Child => "👶 Ребёнок",
      _ => "❓ Неизвестно"
    };
  }

  private async Task HandleFamilyCommandAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    CancellationToken cancellationToken)
  {
    // Get user ID
    var registerCommand = new RegisterUserCommand(message.From!.Id, message.From.GetDisplayName());
    var userResult = await mediator.Send(registerCommand, cancellationToken);

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
    var registerCommand = new RegisterUserCommand(message.From!.Id, message.From.GetDisplayName());
    var userResult = await mediator.Send(registerCommand, cancellationToken);

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
    var registerCommand = new RegisterUserCommand(message.From!.Id, message.From.GetDisplayName());
    var userResult = await mediator.Send(registerCommand, cancellationToken);

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
    var registerCommand = new RegisterUserCommand(message.From!.Id, message.From.GetDisplayName());
    var userResult = await mediator.Send(registerCommand, cancellationToken);

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
    var registerCommand = new RegisterUserCommand(message.From!.Id, message.From.GetDisplayName());
    var userResult = await mediator.Send(registerCommand, cancellationToken);

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
    CancellationToken cancellationToken)
  {
    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      BotConstants.Errors.UnknownCommand,
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
    // Handle location messages
    if (message.Location != null && session.State == ConversationState.AwaitingFamilyLocation)
    {
      await HandleFamilyLocationInputAsync(botClient, message, session, cancellationToken);
      return;
    }

    var text = message.Text!;

    switch (session.State)
    {
      case ConversationState.AwaitingFamilyName:
        await HandleFamilyNameInputAsync(botClient, message, session, text, cancellationToken);
        break;

      case ConversationState.AwaitingFamilyTimezone:
        // Timezone selection is handled via callbacks, not text input
        await botClient.SendTextMessageAsync(
          message.Chat.Id,
          "❌ Пожалуйста, используйте кнопки для выбора временной зоны.",
          cancellationToken: cancellationToken);
        break;

      case ConversationState.AwaitingFamilyLocation:
        // Handle "Back" button
        if (text == "⬅️ Назад")
        {
          await HandleBackToTimezoneSelectionAsync(botClient, message, session, cancellationToken);
          return;
        }

        await botClient.SendTextMessageAsync(
          message.Chat.Id,
          "❌ Пожалуйста, используйте кнопку \"📍 Отправить местоположение\" для определения временной зоны.",
          cancellationToken: cancellationToken);
        break;

      case ConversationState.AwaitingPetName:
        await HandlePetNameInputAsync(botClient, message, session, text, cancellationToken);
        break;

      case ConversationState.AwaitingTaskTitle:
        await HandleTaskTitleInputAsync(botClient, message, session, text, cancellationToken);
        break;

      case ConversationState.AwaitingTaskPoints:
        await HandleTaskPointsInputAsync(botClient, message, session, text, cancellationToken);
        break;

      case ConversationState.AwaitingTaskDueDate:
        await HandleTaskDueDateInputAsync(botClient, message, session, text, cancellationToken);
        break;

      case ConversationState.AwaitingTaskSchedule:
        await HandleTaskScheduleInputAsync(botClient, message, session, text, cancellationToken);
        break;

      case ConversationState.AwaitingTemplateTitle:
        await HandleTemplateTitleInputAsync(botClient, message, session, text, cancellationToken);
        break;

      case ConversationState.AwaitingTemplatePoints:
        await HandleTemplatePointsInputAsync(botClient, message, session, text, cancellationToken);
        break;

      case ConversationState.AwaitingTemplateSchedule:
        await HandleTemplateScheduleInputAsync(botClient, message, session, text, cancellationToken);
        break;

      case ConversationState.AwaitingTemplateEditTitle:
        await HandleTemplateEditTitleInputAsync(botClient, message, session, text, cancellationToken);
        break;

      case ConversationState.AwaitingTemplateEditPoints:
        await HandleTemplateEditPointsInputAsync(botClient, message, session, text, cancellationToken);
        break;

      case ConversationState.AwaitingTemplateEditSchedule:
        await HandleTemplateEditScheduleInputAsync(botClient, message, session, text, cancellationToken);
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

    // Store family name and ask for timezone
    session.Data["familyName"] = familyName;
    session.State = ConversationState.AwaitingFamilyTimezone;

    var keyboard = GetRussianTimeZoneKeyboard();

    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      $"🌍 Выберите вашу временную зону для семьи \"{familyName}\":",
      replyMarkup: keyboard,
      cancellationToken: cancellationToken);
  }

  private async Task HandleFamilyLocationInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    CancellationToken cancellationToken)
  {
    var location = message.Location;

    // Defensive null check
    if (location?.Latitude == null || location?.Longitude == null)
    {
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        "❌ Получены некорректные данные о местоположении.\n\n" +
        BotConstants.Errors.TryAgain,
        replyMarkup: new ReplyKeyboardRemove(),
        cancellationToken: cancellationToken);

      await HandleBackToTimezoneSelectionAsync(botClient, message, session, cancellationToken);
      return;
    }

    try
    {
      // Convert coordinates to timezone using GeoTimeZone
      var timeZoneResult = TimeZoneLookup.GetTimeZone(location.Latitude, location.Longitude);
      var detectedTimezone = timeZoneResult.Result;

      // Add null check for ocean/invalid coordinates
      if (string.IsNullOrEmpty(detectedTimezone))
      {
        await botClient.SendTextMessageAsync(
          message.Chat.Id,
          "❌ Не удалось определить временную зону для вашей локации.\n\n" +
          "Пожалуйста, выберите временную зону вручную.",
          replyMarkup: new ReplyKeyboardRemove(),
          cancellationToken: cancellationToken);

        await HandleBackToTimezoneSelectionAsync(botClient, message, session, cancellationToken);
        return;
      }

      logger.LogInformation("Detected timezone for coordinates {Lat}, {Lng}: {Timezone}",
        location.Latitude, location.Longitude, detectedTimezone);

      // Get required data from session
      if (!session.Data.TryGetValue("userId", out var userIdObj) || userIdObj is not Guid userId ||
          !session.Data.TryGetValue("familyName", out var familyNameObj) || familyNameObj is not string familyName)
      {
        session.ClearState();
        await botClient.SendTextMessageAsync(
          message.Chat.Id,
          "❌ Ошибка сессии. Попробуйте создать семью заново.",
          cancellationToken: cancellationToken);
        return;
      }

      // Validate detected timezone
      if (!timeZoneService.IsValidTimeZone(detectedTimezone))
      {
        await botClient.SendTextMessageAsync(
          message.Chat.Id,
          $"❌ Не удалось определить временную зону для вашей локации.\n\n" +
          BotConstants.Errors.ChooseTimezoneManually,
          replyMarkup: new ReplyKeyboardRemove(),
          cancellationToken: cancellationToken);

        await HandleBackToTimezoneSelectionAsync(botClient, message, session, cancellationToken);
        return;
      }

      // Create family with detected timezone
      var createFamilyCommand = new CreateFamilyCommand(userId, familyName, detectedTimezone);
      var result = await mediator.Send(createFamilyCommand, cancellationToken);

      if (!result.IsSuccess)
      {
        await botClient.SendTextMessageAsync(
          message.Chat.Id,
          $"❌ Ошибка создания семьи: {result.Errors.FirstOrDefault()}",
          replyMarkup: new ReplyKeyboardRemove(),
          cancellationToken: cancellationToken);
        session.ClearState();
        return;
      }

      session.CurrentFamilyId = result.Value;
      session.ClearState();

      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        BotConstants.Success.FamilyCreatedMessage(familyName) +
        $"🌍 Определенная временная зона: {detectedTimezone}\n\n" +
        BotConstants.Success.NextStepsMessage,
        parseMode: ParseMode.Markdown,
        replyMarkup: new ReplyKeyboardRemove(),
        cancellationToken: cancellationToken);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error determining timezone from location");

      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        BotConstants.Errors.LocationError +
        BotConstants.Errors.TryAgainOrChooseTimezone,
        replyMarkup: new ReplyKeyboardRemove(),
        cancellationToken: cancellationToken);

      await HandleBackToTimezoneSelectionAsync(botClient, message, session, cancellationToken);
    }
  }

  private async Task HandleBackToTimezoneSelectionAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    CancellationToken cancellationToken)
  {
    session.State = ConversationState.AwaitingFamilyTimezone;

    var keyboard = GetRussianTimeZoneKeyboard();

    var familyName = session.Data["familyName"] as string ?? "ваша семья";

    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      $"🌍 Выберите вашу временную зону для семьи \"{familyName}\":",
      replyMarkup: keyboard,
      cancellationToken: cancellationToken);
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
    var result = await mediator.Send(createPetCommand, cancellationToken);

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
      BotConstants.Messages.PetTasksAvailable,
      cancellationToken: cancellationToken);
  }

  private async Task HandleTaskTitleInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string title,
    CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(title) || title.Length < 3 || title.Length > 100)
    {
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        "❌ Название задачи должно содержать от 3 до 100 символов. Попробуйте снова:",
        cancellationToken: cancellationToken);
      return;
    }

    // Store title and move to points input
    session.Data["title"] = title;
    session.State = ConversationState.AwaitingTaskPoints;

    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      "💯 Введите количество очков за выполнение задачи (от 1 до 100):",
      cancellationToken: cancellationToken);
  }

  private async Task HandleTaskPointsInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string pointsText,
    CancellationToken cancellationToken)
  {
    if (!int.TryParse(pointsText, out var points) || points < 1 || points > 100)
    {
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        "❌ Количество очков должно быть числом от 1 до 100. Попробуйте снова:",
        cancellationToken: cancellationToken);
      return;
    }

    // Store points and show pet selection
    session.Data["points"] = points;
    session.State = ConversationState.AwaitingTaskPetSelection;

    // Get family pets
    if (!session.Data.TryGetValue("familyId", out var familyIdObj) || familyIdObj is not Guid familyId)
    {
      session.ClearState();
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        "❌ Ошибка. Попробуйте создать задачу заново.",
        cancellationToken: cancellationToken);
      return;
    }

    var getPetsQuery = new GetPetsQuery(familyId);
    var petsResult = await mediator.Send(getPetsQuery, cancellationToken);

    if (!petsResult.IsSuccess || !petsResult.Value.Any())
    {
      session.ClearState();
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        BotConstants.Errors.NoPets,
        cancellationToken: cancellationToken);
      return;
    }

    var buttons = petsResult.Value.Select(p =>
    {
      var petEmoji = p.Type switch
      {
        PetType.Cat => "🐱",
        PetType.Dog => "🐶",
        PetType.Hamster => "🐹",
        _ => "🐾"
      };
      return new[] { InlineKeyboardButton.WithCallbackData($"{petEmoji} {p.Name}", $"taskpet_{p.Id}") };
    }).ToArray();

    var keyboard = new InlineKeyboardMarkup(buttons);

    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      "🐾 Выберите питомца, к которому относится задача:",
      replyMarkup: keyboard,
      cancellationToken: cancellationToken);
  }

  private async Task HandleTaskDueDateInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string dueDateText,
    CancellationToken cancellationToken)
  {
    // Try to parse the date
    if (!int.TryParse(dueDateText, out var days) || days < 0 || days > 365)
    {
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        "❌ Введите количество дней (от 0 до 365). Например: 1 (завтра), 7 (через неделю):",
        cancellationToken: cancellationToken);
      return;
    }

    var dueAt = DateTime.UtcNow.AddDays(days);

    // Get all required data from session
    if (!session.Data.TryGetValue("familyId", out var familyIdObj) || familyIdObj is not Guid familyId ||
        !session.Data.TryGetValue("petId", out var petIdObj) || petIdObj is not Guid petId ||
        !session.Data.TryGetValue("title", out var titleObj) || titleObj is not string title ||
        !session.Data.TryGetValue("points", out var pointsObj) || pointsObj is not int points)
    {
      session.ClearState();
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        "❌ Ошибка. Попробуйте создать задачу заново.",
        cancellationToken: cancellationToken);
      return;
    }

    // Get user ID
    var registerCommand = new RegisterUserCommand(message.From!.Id, message.From.GetDisplayName());
    var userResult = await mediator.Send(registerCommand, cancellationToken);

    if (!userResult.IsSuccess)
    {
      session.ClearState();
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        "❌ Ошибка. Попробуйте /start",
        cancellationToken: cancellationToken);
      return;
    }

    // Create one-time task
    var createTaskCommand = new CreateTaskCommand(familyId, petId, title, points, dueAt, userResult.Value);
    var result = await mediator.Send(createTaskCommand, cancellationToken);

    if (!result.IsSuccess)
    {
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        $"❌ Ошибка создания задачи: {result.Errors.FirstOrDefault()}",
        cancellationToken: cancellationToken);
      session.ClearState();
      return;
    }

    session.ClearState();

    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      $"✅ Задача \"{title}\" успешно создана!\n\n" +
      $"💯 Очки: {points}\n" +
      $"📅 Срок: {dueAt:dd.MM.yyyy HH:mm}\n\n" +
      BotConstants.Messages.TaskAvailableToAll,
      cancellationToken: cancellationToken);
  }

  private async Task HandleTaskScheduleInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string schedule,
    CancellationToken cancellationToken)
  {
    // Validate schedule (basic check)
    if (string.IsNullOrWhiteSpace(schedule))
    {
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        "❌ Расписание не может быть пустым. Попробуйте снова:",
        cancellationToken: cancellationToken);
      return;
    }

    // Get all required data from session
    if (!session.Data.TryGetValue("familyId", out var familyIdObj) || familyIdObj is not Guid familyId ||
        !session.Data.TryGetValue("petId", out var petIdObj) || petIdObj is not Guid petId ||
        !session.Data.TryGetValue("title", out var titleObj) || titleObj is not string title ||
        !session.Data.TryGetValue("points", out var pointsObj) || pointsObj is not int points)
    {
      session.ClearState();
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        "❌ Ошибка. Попробуйте создать задачу заново.",
        cancellationToken: cancellationToken);
      return;
    }

    // Get user ID
    var registerCommand = new RegisterUserCommand(message.From!.Id, message.From.GetDisplayName());
    var userResult = await mediator.Send(registerCommand, cancellationToken);

    if (!userResult.IsSuccess)
    {
      session.ClearState();
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        "❌ Ошибка. Попробуйте /start",
        cancellationToken: cancellationToken);
      return;
    }

    // Create periodic task template
    var createTemplateCommand =
      new CreateTaskTemplateCommand(familyId, petId, title, points, schedule, userResult.Value);
    var result = await mediator.Send(createTemplateCommand, cancellationToken);

    if (!result.IsSuccess)
    {
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        $"❌ Ошибка создания задачи: {result.Errors.FirstOrDefault()}\n\n" +
        BotConstants.Errors.InvalidCron,
        cancellationToken: cancellationToken);
      session.ClearState();
      return;
    }

    session.ClearState();

    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      $"✅ Периодическая задача \"{title}\" успешно создана!\n\n" +
      $"💯 Очки: {points}\n" +
      $"🔄 Расписание: {schedule}\n\n" +
      BotConstants.Messages.ScheduledTask,
      cancellationToken: cancellationToken);
  }

  private async Task HandleTemplateTitleInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string title,
    CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(title) || title.Length < 3 || title.Length > 100)
    {
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        "❌ Название шаблона должно содержать от 3 до 100 символов. Попробуйте снова:",
        cancellationToken: cancellationToken);
      return;
    }

    session.Data["title"] = title;
    session.State = ConversationState.AwaitingTemplatePoints;

    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      BotConstants.Templates.EnterTemplatePoints,
      cancellationToken: cancellationToken);
  }

  private async Task HandleTemplatePointsInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string pointsText,
    CancellationToken cancellationToken)
  {
    if (!int.TryParse(pointsText, out var points) || points < 1 || points > 100)
    {
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        "❌ Количество очков должно быть числом от 1 до 100. Попробуйте снова:",
        cancellationToken: cancellationToken);
      return;
    }

    session.Data["points"] = points;
    session.State = ConversationState.AwaitingTemplateSchedule;

    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      BotConstants.Templates.EnterTemplateSchedule,
      parseMode: ParseMode.Markdown,
      cancellationToken: cancellationToken);
  }

  private async Task HandleTemplateScheduleInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string schedule,
    CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(schedule))
    {
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        "❌ Расписание не может быть пустым. Попробуйте снова:",
        cancellationToken: cancellationToken);
      return;
    }

    // Get all required data from session
    if (!session.Data.TryGetValue("familyId", out var familyIdObj) || familyIdObj is not Guid familyId ||
        !session.Data.TryGetValue("petId", out var petIdObj) || petIdObj is not Guid petId ||
        !session.Data.TryGetValue("title", out var titleObj) || titleObj is not string title ||
        !session.Data.TryGetValue("points", out var pointsObj) || pointsObj is not int points)
    {
      session.ClearState();
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        "❌ Ошибка. Попробуйте создать шаблон заново.",
        cancellationToken: cancellationToken);
      return;
    }

    // Get user ID
    var registerCommand = new RegisterUserCommand(message.From!.Id, message.From.GetDisplayName());
    var userResult = await mediator.Send(registerCommand, cancellationToken);

    if (!userResult.IsSuccess)
    {
      session.ClearState();
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        "❌ Ошибка. Попробуйте /start",
        cancellationToken: cancellationToken);
      return;
    }

    // Create template
    var createTemplateCommand =
      new CreateTaskTemplateCommand(familyId, petId, title, points, schedule, userResult.Value);
    var result = await mediator.Send(createTemplateCommand, cancellationToken);

    if (!result.IsSuccess)
    {
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        $"❌ Ошибка создания шаблона: {result.Errors.FirstOrDefault()}\n\n" +
        BotConstants.Errors.InvalidCron,
        cancellationToken: cancellationToken);
      session.ClearState();
      return;
    }

    session.ClearState();

    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      $"{BotConstants.Templates.TemplateCreated}\n\n" +
      $"📝 Название: {title}\n" +
      $"💯 Очки: {points}\n" +
      $"🔄 Расписание: {schedule}\n\n" +
      BotConstants.Messages.ScheduledTask,
      cancellationToken: cancellationToken);
  }

  private async Task HandleTemplateEditTitleInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string title,
    CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(title) || title.Length < 3 || title.Length > 100)
    {
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        "❌ Название шаблона должно содержать от 3 до 100 символов. Попробуйте снова:",
        cancellationToken: cancellationToken);
      return;
    }

    if (!session.Data.TryGetValue("templateId", out var templateIdObj) || templateIdObj is not Guid templateId ||
        !session.Data.TryGetValue("familyId", out var familyIdObj) || familyIdObj is not Guid familyId)
    {
      session.ClearState();
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        "❌ Ошибка. Попробуйте снова.",
        cancellationToken: cancellationToken);
      return;
    }

    var updateCommand = new UpdateTaskTemplateCommand(templateId, familyId, title, null, null);
    var result = await mediator.Send(updateCommand, cancellationToken);

    if (!result.IsSuccess)
    {
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        $"❌ Ошибка обновления: {result.Errors.FirstOrDefault()}",
        cancellationToken: cancellationToken);
      session.ClearState();
      return;
    }

    session.ClearState();
    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      BotConstants.Templates.TemplateUpdated,
      cancellationToken: cancellationToken);
  }

  private async Task HandleTemplateEditPointsInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string pointsText,
    CancellationToken cancellationToken)
  {
    if (!int.TryParse(pointsText, out var points) || points < 1 || points > 100)
    {
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        "❌ Количество очков должно быть числом от 1 до 100. Попробуйте снова:",
        cancellationToken: cancellationToken);
      return;
    }

    if (!session.Data.TryGetValue("templateId", out var templateIdObj) || templateIdObj is not Guid templateId ||
        !session.Data.TryGetValue("familyId", out var familyIdObj) || familyIdObj is not Guid familyId)
    {
      session.ClearState();
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        "❌ Ошибка. Попробуйте снова.",
        cancellationToken: cancellationToken);
      return;
    }

    var updateCommand = new UpdateTaskTemplateCommand(templateId, familyId, null, points, null);
    var result = await mediator.Send(updateCommand, cancellationToken);

    if (!result.IsSuccess)
    {
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        $"❌ Ошибка обновления: {result.Errors.FirstOrDefault()}",
        cancellationToken: cancellationToken);
      session.ClearState();
      return;
    }

    session.ClearState();
    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      BotConstants.Templates.TemplateUpdated,
      cancellationToken: cancellationToken);
  }

  private async Task HandleTemplateEditScheduleInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string schedule,
    CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(schedule))
    {
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        "❌ Расписание не может быть пустым. Попробуйте снова:",
        cancellationToken: cancellationToken);
      return;
    }

    if (!session.Data.TryGetValue("templateId", out var templateIdObj) || templateIdObj is not Guid templateId ||
        !session.Data.TryGetValue("familyId", out var familyIdObj) || familyIdObj is not Guid familyId)
    {
      session.ClearState();
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        "❌ Ошибка. Попробуйте снова.",
        cancellationToken: cancellationToken);
      return;
    }

    var updateCommand = new UpdateTaskTemplateCommand(templateId, familyId, null, null, schedule);
    var result = await mediator.Send(updateCommand, cancellationToken);

    if (!result.IsSuccess)
    {
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        $"❌ Ошибка обновления: {result.Errors.FirstOrDefault()}\n\n" +
        BotConstants.Errors.InvalidCron,
        cancellationToken: cancellationToken);
      session.ClearState();
      return;
    }

    session.ClearState();
    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      BotConstants.Templates.TemplateUpdated,
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
