using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.Interfaces;
using FamilyTaskManager.Host.Modules.Bot.Helpers;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.Host.Modules.Bot.Services;
using FamilyTaskManager.UseCases.Families;
using FamilyTaskManager.UseCases.Tasks;
using FamilyTaskManager.UseCases.Users;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers;

public class CallbackQueryHandler(
  ILogger<CallbackQueryHandler> logger,
  ISessionManager sessionManager,
  IMediator mediator,
  ITimeZoneService timeZoneService)
  : ICallbackQueryHandler
{
  public async Task HandleCallbackAsync(
    ITelegramBotClient botClient,
    CallbackQuery callbackQuery,
    CancellationToken cancellationToken)
  {
    var telegramId = callbackQuery.From.Id;
    var session = sessionManager.GetSession(telegramId);
    session.UpdateActivity();

    var data = callbackQuery.Data!;
    var chatId = callbackQuery.Message!.Chat.Id;
    var messageId = callbackQuery.Message.MessageId;

    try
    {
      // Answer callback query to remove loading state
      await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);

      // Parse callback data
      var parts = data.Split('_');
      var action = parts[0];

      await (action switch
      {
        "create" => HandleCreateActionAsync(botClient, chatId, messageId, parts, session, callbackQuery.From,
          cancellationToken),
        "select" => HandleSelectActionAsync(botClient, chatId, messageId, parts, session, cancellationToken),
        "task" => HandleTaskActionAsync(botClient, chatId, messageId, parts, session, callbackQuery.From,
          cancellationToken),
        "taskpet" => HandleTaskPetSelectionAsync(botClient, chatId, messageId, parts, session, cancellationToken),
        "pet" => HandlePetActionAsync(botClient, chatId, messageId, parts, session, cancellationToken),
        "family" => HandleFamilyActionAsync(botClient, chatId, messageId, parts, session, callbackQuery.From,
          cancellationToken),
        "invite" => HandleInviteActionAsync(botClient, chatId, messageId, parts, session, callbackQuery.From,
          cancellationToken),
        "timezone" => HandleTimezoneSelectionAsync(botClient, chatId, messageId, parts, session, cancellationToken),
        "confirm" => HandleConfirmActionAsync(botClient, chatId, messageId, parts, session, callbackQuery.From,
          cancellationToken),
        "cancel" => HandleCancelActionAsync(botClient, chatId, messageId, parts, session, cancellationToken),
        _ => HandleUnknownCallbackAsync(botClient, chatId, cancellationToken)
      });
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
        await StartCreateFamilyAsync(botClient, chatId, messageId, session, fromUser, cancellationToken);
        break;

      case "pet":
        await StartCreatePetAsync(botClient, chatId, messageId, session, cancellationToken);
        break;

      case "task":
        await StartCreateTaskAsync(botClient, chatId, messageId, session, cancellationToken);
        break;
    }
  }

  private async Task StartCreateFamilyAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    UserSession session,
    User fromUser,
    CancellationToken cancellationToken)
  {
    // Get user by telegram ID
    var registerCommand = new RegisterUserCommand(fromUser.Id, fromUser.GetDisplayName());
    var userResult = await mediator.Send(registerCommand, cancellationToken);

    if (!userResult.IsSuccess)
    {
      await botClient.SendTextMessageAsync(
        chatId,
        "❌ Ошибка. Попробуйте /start",
        cancellationToken: cancellationToken);
      return;
    }

    session.SetState(ConversationState.AwaitingFamilyName,
      new Dictionary<string, object> { ["userId"] = userResult.Value });

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      "✏️ Введите название семьи (минимум 3 символа):",
      cancellationToken: cancellationToken);
  }

  private async Task StartCreatePetAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    UserSession session,
    CancellationToken cancellationToken)
  {
    if (session.CurrentFamilyId == null)
    {
      await botClient.SendTextMessageAsync(
        chatId,
        "❌ Сначала выберите активную семью",
        cancellationToken: cancellationToken);
      return;
    }

    var keyboard = new InlineKeyboardMarkup(new[]
    {
      new[] { InlineKeyboardButton.WithCallbackData("🐱 Кот", "select_pettype_cat") },
      new[] { InlineKeyboardButton.WithCallbackData("🐶 Собака", "select_pettype_dog") },
      new[] { InlineKeyboardButton.WithCallbackData("🐹 Хомяк", "select_pettype_hamster") }
    });

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      "🐾 Выберите тип питомца:",
      replyMarkup: keyboard,
      cancellationToken: cancellationToken);
  }

  private async Task StartCreateTaskAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    UserSession session,
    CancellationToken cancellationToken)
  {
    if (session.CurrentFamilyId == null)
    {
      await botClient.SendTextMessageAsync(
        chatId,
        "❌ Сначала выберите активную семью",
        cancellationToken: cancellationToken);
      return;
    }

    // Ask user to select task type
    var keyboard = new InlineKeyboardMarkup(new[]
    {
      new[] { InlineKeyboardButton.WithCallbackData("📝 Разовая задача", "select_tasktype_onetime") },
      new[] { InlineKeyboardButton.WithCallbackData("🔄 Периодическая задача", "select_tasktype_recurring") }
    });

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      "📋 *Создание задачи*\n\nВыберите тип задачи:",
      ParseMode.Markdown,
      replyMarkup: keyboard,
      cancellationToken: cancellationToken);
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
        await HandlePetTypeSelectionAsync(botClient, chatId, messageId, value, session, cancellationToken);
        break;

      case "family":
        await HandleFamilySelectionAsync(botClient, chatId, messageId, value, session, cancellationToken);
        break;

      case "tasktype":
        await HandleTaskTypeSelectionAsync(botClient, chatId, messageId, value, session, cancellationToken);
        break;
    }
  }

  private async Task HandlePetTypeSelectionAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    string petType,
    UserSession session,
    CancellationToken cancellationToken)
  {
    session.SetState(ConversationState.AwaitingPetName,
      new Dictionary<string, object> { ["petType"] = petType, ["familyId"] = session.CurrentFamilyId! });

    var petTypeEmoji = petType switch
    {
      "cat" => "🐱",
      "dog" => "🐶",
      "hamster" => "🐹",
      _ => "🐾"
    };

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      $"{petTypeEmoji} Введите имя питомца:",
      cancellationToken: cancellationToken);
  }

  private async Task HandleFamilySelectionAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    string familyIdStr,
    UserSession session,
    CancellationToken cancellationToken)
  {
    if (!Guid.TryParse(familyIdStr, out var familyId))
    {
      return;
    }

    session.CurrentFamilyId = familyId;

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      "✅ Семья выбрана!",
      cancellationToken: cancellationToken);
  }

  private async Task HandleTaskActionAsync(
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

    var taskAction = parts[1];
    var taskIdStr = parts[2];

    if (!Guid.TryParse(taskIdStr, out var taskId))
    {
      return;
    }

    switch (taskAction)
    {
      case "take":
        await HandleTakeTaskAsync(botClient, chatId, messageId, taskId, session, fromUser, cancellationToken);
        break;

      case "complete":
        await HandleCompleteTaskAsync(botClient, chatId, messageId, taskId, session, fromUser, cancellationToken);
        break;
    }
  }

  private async Task HandleTakeTaskAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    Guid taskId,
    UserSession session,
    User fromUser,
    CancellationToken cancellationToken)
  {
    // Get user by telegram ID
    var registerCommand = new RegisterUserCommand(fromUser.Id, fromUser.GetDisplayName());
    var userResult = await mediator.Send(registerCommand, cancellationToken);

    if (!userResult.IsSuccess)
    {
      await botClient.SendTextMessageAsync(
        chatId,
        "❌ Ошибка. Попробуйте /start",
        cancellationToken: cancellationToken);
      return;
    }

    // Take task
    var takeTaskCommand = new TakeTaskCommand(taskId, userResult.Value);
    var result = await mediator.Send(takeTaskCommand, cancellationToken);

    if (!result.IsSuccess)
    {
      await botClient.SendTextMessageAsync(
        chatId,
        $"❌ Ошибка: {result.Errors.FirstOrDefault()}",
        cancellationToken: cancellationToken);
      return;
    }

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      "✅ Задача взята в работу!\n\nТеперь вы можете её выполнить.",
      cancellationToken: cancellationToken);
  }

  private async Task HandleCompleteTaskAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    Guid taskId,
    UserSession session,
    User fromUser,
    CancellationToken cancellationToken)
  {
    // Get user by telegram ID
    var registerCommand = new RegisterUserCommand(fromUser.Id, fromUser.GetDisplayName());
    var userResult = await mediator.Send(registerCommand, cancellationToken);

    if (!userResult.IsSuccess)
    {
      await botClient.SendTextMessageAsync(
        chatId,
        "❌ Ошибка. Попробуйте /start",
        cancellationToken: cancellationToken);
      return;
    }

    // Complete task
    var completeTaskCommand = new CompleteTaskCommand(taskId, userResult.Value);
    var result = await mediator.Send(completeTaskCommand, cancellationToken);

    if (!result.IsSuccess)
    {
      await botClient.SendTextMessageAsync(
        chatId,
        $"❌ Ошибка: {result.Errors.FirstOrDefault()}",
        cancellationToken: cancellationToken);
      return;
    }

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      "🎉 Задача выполнена!\n\n⭐ Очки начислены!",
      cancellationToken: cancellationToken);

    // TODO: Send notification to all family members
  }

  private async Task HandlePetActionAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    string[] parts,
    UserSession session,
    CancellationToken cancellationToken)
  {
    await botClient.SendTextMessageAsync(
      chatId,
      "🐾 Действие с питомцем\n(В разработке)",
      cancellationToken: cancellationToken);
  }

  private async Task HandleFamilyActionAsync(
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

    var familyAction = parts[1];
    var familyIdStr = parts[2];

    if (!Guid.TryParse(familyIdStr, out var familyId))
    {
      return;
    }

    switch (familyAction)
    {
      case "invite":
        await HandleCreateInviteAsync(botClient, chatId, messageId, familyId, session, fromUser, cancellationToken);
        break;

      case "members":
        await HandleFamilyMembersAsync(botClient, chatId, messageId, familyId, cancellationToken);
        break;

      case "settings":
        await HandleFamilySettingsAsync(botClient, chatId, messageId, familyId, cancellationToken);
        break;

      case "delete":
        await HandleDeleteFamilyAsync(botClient, chatId, messageId, familyId, session, fromUser, cancellationToken);
        break;

      default:
        await botClient.SendTextMessageAsync(
          chatId,
          "🏠 Действие с семьей\n(В разработке)",
          cancellationToken: cancellationToken);
        break;
    }
  }

  private async Task HandleCreateInviteAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    Guid familyId,
    UserSession session,
    User fromUser,
    CancellationToken cancellationToken)
  {
    // Get user by telegram ID
    var registerCommand = new RegisterUserCommand(fromUser.Id, fromUser.GetDisplayName());
    var userResult = await mediator.Send(registerCommand, cancellationToken);

    if (!userResult.IsSuccess)
    {
      await botClient.SendTextMessageAsync(
        chatId,
        "❌ Ошибка. Попробуйте /start",
        cancellationToken: cancellationToken);
      return;
    }

    // Show role selection
    var keyboard = new InlineKeyboardMarkup(new[]
    {
      new[] { InlineKeyboardButton.WithCallbackData("👑 Администратор", $"invite_role_{familyId}_Admin") },
      new[] { InlineKeyboardButton.WithCallbackData("👤 Взрослый", $"invite_role_{familyId}_Adult") },
      new[] { InlineKeyboardButton.WithCallbackData("👶 Ребёнок", $"invite_role_{familyId}_Child") }
    });

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      "🔗 *Создание приглашения*\n\nВыберите роль для нового участника:",
      ParseMode.Markdown,
      replyMarkup: keyboard,
      cancellationToken: cancellationToken);
  }

  private async Task HandleFamilyMembersAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    Guid familyId,
    CancellationToken cancellationToken)
  {
    await botClient.SendTextMessageAsync(
      chatId,
      "👥 Управление участниками\n(В разработке)",
      cancellationToken: cancellationToken);
  }

  private async Task HandleFamilySettingsAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    Guid familyId,
    CancellationToken cancellationToken)
  {
    await botClient.SendTextMessageAsync(
      chatId,
      "⚙️ Настройки семьи\n(В разработке)",
      cancellationToken: cancellationToken);
  }

  private async Task HandleDeleteFamilyAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    Guid familyId,
    UserSession session,
    User fromUser,
    CancellationToken cancellationToken)
  {
    // Get user by telegram ID
    var registerCommand = new RegisterUserCommand(fromUser.Id, fromUser.GetDisplayName());
    var userResult = await mediator.Send(registerCommand, cancellationToken);

    if (!userResult.IsSuccess)
    {
      await botClient.SendTextMessageAsync(
        chatId,
        "❌ Ошибка. Попробуйте /start",
        cancellationToken: cancellationToken);
      return;
    }

    // Show confirmation dialog
    var keyboard = new InlineKeyboardMarkup(new[]
    {
      new[] { InlineKeyboardButton.WithCallbackData("✅ Да, удалить семью", $"confirm_delete_{familyId}") },
      new[] { InlineKeyboardButton.WithCallbackData("❌ Отмена", "cancel_delete") }
    });

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      "⚠️ *Удаление семьи*\n\n" +
      "Вы уверены, что хотите удалить эту семью?\n\n" +
      "🚨 *Внимание!* Это действие необратимо и приведет к:\n" +
      "• Удалению всех участников семьи\n" +
      "• Удалению всех питомцев\n" +
      "• Удалению всех задач и их истории\n" +
      "• Удалению всей статистики\n\n" +
      "Подтвердите удаление:",
      ParseMode.Markdown,
      replyMarkup: keyboard,
      cancellationToken: cancellationToken);
  }

  private async Task HandleInviteActionAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    string[] parts,
    UserSession session,
    User fromUser,
    CancellationToken cancellationToken)
  {
    if (parts.Length < 4)
    {
      return;
    }

    var inviteAction = parts[1];
    var familyIdStr = parts[2];
    var roleStr = parts[3];

    if (!Guid.TryParse(familyIdStr, out var familyId))
    {
      return;
    }

    if (!Enum.TryParse<FamilyRole>(roleStr, out var role))
    {
      return;
    }

    // Get user by telegram ID
    var registerCommand = new RegisterUserCommand(fromUser.Id, fromUser.GetDisplayName());
    var userResult = await mediator.Send(registerCommand, cancellationToken);

    if (!userResult.IsSuccess)
    {
      await botClient.SendTextMessageAsync(
        chatId,
        "❌ Ошибка. Попробуйте /start",
        cancellationToken: cancellationToken);
      return;
    }

    // Create invite code
    var createInviteCommand = new CreateInviteCodeCommand(familyId, role, userResult.Value);
    var result = await mediator.Send(createInviteCommand, cancellationToken);

    if (!result.IsSuccess)
    {
      await botClient.SendTextMessageAsync(
        chatId,
        $"❌ Ошибка: {result.Errors.FirstOrDefault()}",
        cancellationToken: cancellationToken);
      return;
    }

    var inviteCode = result.Value;
    var botUsername = "YourBotUsername"; // TODO: Get from configuration
    var inviteLink = $"https://t.me/{botUsername}?start=invite_{inviteCode}";

    var roleText = role switch
    {
      FamilyRole.Admin => "Администратор",
      FamilyRole.Adult => "Взрослый",
      FamilyRole.Child => "Ребёнок",
      _ => "Неизвестно"
    };

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      $"✅ *Приглашение создано!*\n\n" +
      $"🔗 Ссылка для приглашения:\n{inviteLink}\n\n" +
      $"👤 Роль: {roleText}\n" +
      $"🔑 Код: `{inviteCode}`\n" +
      $"⏰ Действительно 7 дней\n\n" +
      $"Отправьте эту ссылку человеку, которого хотите пригласить в семью.",
      ParseMode.Markdown,
      cancellationToken: cancellationToken);
  }

  private async Task HandleTaskTypeSelectionAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    string taskType,
    UserSession session,
    CancellationToken cancellationToken)
  {
    // Store task type in session
    session.SetState(ConversationState.AwaitingTaskTitle,
      new Dictionary<string, object> { ["taskType"] = taskType, ["familyId"] = session.CurrentFamilyId! });

    var taskTypeText = taskType == "onetime" ? "разовую" : "периодическую";

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      $"📝 Создание {taskTypeText} задачи\n\nВведите название задачи (от 3 до 100 символов):",
      cancellationToken: cancellationToken);
  }

  private async Task HandleTaskPetSelectionAsync(
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

    if (!Guid.TryParse(parts[1], out var petId))
    {
      return;
    }

    // Store pet ID in session
    session.Data["petId"] = petId;

    // Check task type to determine next step
    if (!session.Data.TryGetValue("taskType", out var taskTypeObj) || taskTypeObj is not string taskType)
    {
      session.ClearState();
      await botClient.SendTextMessageAsync(
        chatId,
        "❌ Ошибка. Попробуйте создать задачу заново.",
        cancellationToken: cancellationToken);
      return;
    }

    if (taskType == "onetime")
    {
      // For one-time tasks, ask for due date
      session.State = ConversationState.AwaitingTaskDueDate;

      await botClient.EditMessageTextAsync(
        chatId,
        messageId,
        "📅 Введите срок выполнения задачи в днях:\n\n" +
        "0 - сегодня\n" +
        "1 - завтра\n" +
        "7 - через неделю\n" +
        "30 - через месяц",
        cancellationToken: cancellationToken);
    }
    else
    {
      // For recurring tasks, ask for schedule
      session.State = ConversationState.AwaitingTaskSchedule;

      await botClient.EditMessageTextAsync(
        chatId,
        messageId,
        "🔄 Введите расписание задачи в формате Quartz Cron:\n\n" +
        "Примеры:\n" +
        "• `0 0 9 * * ?` - ежедневно в 9:00\n" +
        "• `0 0 20 * * ?` - ежедневно в 20:00\n" +
        "• `0 0 9 */5 * ?` - каждые 5 дней в 9:00\n" +
        "• `0 0 9 * * MON` - каждый понедельник в 9:00",
        ParseMode.Markdown,
        cancellationToken: cancellationToken);
    }
  }

  private async Task HandleTimezoneSelectionAsync(
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

    var timezoneId = parts[1];

    // Handle geolocation detection request
    if (timezoneId == "detect")
    {
      session.State = ConversationState.AwaitingFamilyLocation;

      var locationKeyboard = new ReplyKeyboardMarkup(new[]
      {
        new KeyboardButton("📍 Отправить местоположение") { RequestLocation = true }, new KeyboardButton("⬅️ Назад")
      }) { ResizeKeyboard = true, OneTimeKeyboard = true };

      await botClient.EditMessageTextAsync(
        chatId,
        messageId,
        "📍 Нажмите кнопку ниже, чтобы поделиться местоположением:",
        cancellationToken: cancellationToken);

      await botClient.SendTextMessageAsync(
        chatId,
        "🌍 Определение временной зоны по геолокации\n\n" +
        "Нажмите \"📍 Отправить местоположение\" для автоматического определения, " +
        "или \"⬅️ Назад\" для выбора вручную.",
        replyMarkup: locationKeyboard,
        cancellationToken: cancellationToken);
      return;
    }

    // Get required data from session
    if (!session.Data.TryGetValue("userId", out var userIdObj) || userIdObj is not Guid userId ||
        !session.Data.TryGetValue("familyName", out var familyNameObj) || familyNameObj is not string familyName)
    {
      session.ClearState();
      await botClient.EditMessageTextAsync(
        chatId,
        messageId,
        "❌ Ошибка сессии. Попробуйте создать семью заново.",
        cancellationToken: cancellationToken);
      return;
    }

    // Validate timezone
    if (!timeZoneService.IsValidTimeZone(timezoneId))
    {
      await botClient.EditMessageTextAsync(
        chatId,
        messageId,
        "❌ Неверная временная зона. Попробуйте снова.",
        cancellationToken: cancellationToken);
      return;
    }

    // Create family with selected timezone
    var createFamilyCommand = new CreateFamilyCommand(userId, familyName, timezoneId);
    var result = await mediator.Send(createFamilyCommand, cancellationToken);

    if (!result.IsSuccess)
    {
      await botClient.EditMessageTextAsync(
        chatId,
        messageId,
        $"❌ Ошибка создания семьи: {result.Errors.FirstOrDefault()}",
        cancellationToken: cancellationToken);
      session.ClearState();
      return;
    }

    session.CurrentFamilyId = result.Value;
    session.ClearState();

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      $"✅ Семья \"{familyName}\" успешно создана!\n\n" +
      $"🌍 Временная зона: {timezoneId}\n\n" +
      "Теперь вы можете добавить питомца и создать задачи.",
      cancellationToken: cancellationToken);
  }

  private async Task HandleUnknownCallbackAsync(
    ITelegramBotClient botClient,
    long chatId,
    CancellationToken cancellationToken)
  {
    await botClient.SendTextMessageAsync(
      chatId,
      "❓ Неизвестное действие",
      cancellationToken: cancellationToken);
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
      await HandleConfirmDeleteFamilyAsync(botClient, chatId, messageId, familyId, session, fromUser,
        cancellationToken);
    }
  }

  private async Task HandleCancelActionAsync(
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

  private async Task HandleConfirmDeleteFamilyAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    Guid familyId,
    UserSession session,
    User fromUser,
    CancellationToken cancellationToken)
  {
    // Get user by telegram ID
    var registerCommand = new RegisterUserCommand(fromUser.Id, fromUser.GetDisplayName());
    var userResult = await mediator.Send(registerCommand, cancellationToken);

    if (!userResult.IsSuccess)
    {
      await botClient.SendTextMessageAsync(
        chatId,
        "❌ Ошибка. Попробуйте /start",
        cancellationToken: cancellationToken);
      return;
    }

    // Delete the family
    var deleteFamilyCommand = new DeleteFamilyCommand(familyId, userResult.Value);
    var deleteResult = await mediator.Send(deleteFamilyCommand, cancellationToken);

    if (!deleteResult.IsSuccess)
    {
      await botClient.EditMessageTextAsync(
        chatId,
        messageId,
        $"❌ Ошибка удаления семьи: {deleteResult.Errors.FirstOrDefault()}",
        cancellationToken: cancellationToken);
      return;
    }

    // Clear current family if it was the deleted one
    if (session.CurrentFamilyId == familyId)
    {
      session.CurrentFamilyId = null;

      // Try to select another family if user has any remaining
      var getFamiliesQuery = new GetUserFamiliesQuery(userResult.Value);
      var familiesResult = await mediator.Send(getFamiliesQuery, cancellationToken);

      if (familiesResult.IsSuccess && familiesResult.Value.Any())
      {
        session.CurrentFamilyId = familiesResult.Value.First().Id;
      }
    }

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      "✅ Семья успешно удалена!\n\n" +
      "Все данные семьи, включая участников, питомцев, задачи и статистику, были безвозвратно удалены.",
      cancellationToken: cancellationToken);
  }
}
