using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using FamilyTaskManager.Host.Modules.Bot.Services;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.UseCases.Users;
using FamilyTaskManager.UseCases.Users.Specifications;
using FamilyTaskManager.UseCases.Families;
using FamilyTaskManager.UseCases.Pets;
using FamilyTaskManager.UseCases.Tasks;
using Mediator;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers;

public class CallbackQueryHandler : ICallbackQueryHandler
{
  private readonly ILogger<CallbackQueryHandler> _logger;
  private readonly ISessionManager _sessionManager;
  private readonly IMediator _mediator;

  public CallbackQueryHandler(
    ILogger<CallbackQueryHandler> logger,
    ISessionManager sessionManager,
    IMediator mediator)
  {
    _logger = logger;
    _sessionManager = sessionManager;
    _mediator = mediator;
  }

  public async Task HandleCallbackAsync(
    ITelegramBotClient botClient,
    CallbackQuery callbackQuery,
    CancellationToken cancellationToken)
  {
    var telegramId = callbackQuery.From.Id;
    var session = _sessionManager.GetSession(telegramId);
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
        "create" => HandleCreateActionAsync(botClient, chatId, messageId, parts, session, cancellationToken),
        "select" => HandleSelectActionAsync(botClient, chatId, messageId, parts, session, cancellationToken),
        "task" => HandleTaskActionAsync(botClient, chatId, messageId, parts, session, cancellationToken),
        "pet" => HandlePetActionAsync(botClient, chatId, messageId, parts, session, cancellationToken),
        "family" => HandleFamilyActionAsync(botClient, chatId, messageId, parts, session, cancellationToken),
        _ => HandleUnknownCallbackAsync(botClient, chatId, cancellationToken)
      });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error handling callback: {Data}", data);
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
    CancellationToken cancellationToken)
  {
    if (parts.Length < 2)
      return;

    var entityType = parts[1];

    switch (entityType)
    {
      case "family":
        await StartCreateFamilyAsync(botClient, chatId, messageId, session, cancellationToken);
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
    CancellationToken cancellationToken)
  {
    // Get user by telegram ID
    var registerCommand = new RegisterUserCommand(chatId, "User");
    var userResult = await _mediator.Send(registerCommand, cancellationToken);

    if (!userResult.IsSuccess)
    {
      await botClient.SendTextMessageAsync(
        chatId,
        "❌ Ошибка. Попробуйте /start",
        cancellationToken: cancellationToken);
      return;
    }

    session.SetState(ConversationState.AwaitingFamilyName, new Dictionary<string, object>
    {
      ["userId"] = userResult.Value
    });

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

    await botClient.SendTextMessageAsync(
      chatId,
      "✅ Создание задачи\n(В разработке)",
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
      return;

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
    session.SetState(ConversationState.AwaitingPetName, new Dictionary<string, object>
    {
      ["petType"] = petType,
      ["familyId"] = session.CurrentFamilyId!
    });

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
      return;

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
    CancellationToken cancellationToken)
  {
    if (parts.Length < 3)
      return;

    var taskAction = parts[1];
    var taskIdStr = parts[2];

    if (!Guid.TryParse(taskIdStr, out var taskId))
      return;

    switch (taskAction)
    {
      case "take":
        await HandleTakeTaskAsync(botClient, chatId, messageId, taskId, session, cancellationToken);
        break;
      
      case "complete":
        await HandleCompleteTaskAsync(botClient, chatId, messageId, taskId, session, cancellationToken);
        break;
    }
  }

  private async Task HandleTakeTaskAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    Guid taskId,
    UserSession session,
    CancellationToken cancellationToken)
  {
    // Get user by telegram ID
    var registerCommand = new RegisterUserCommand(chatId, "User");
    var userResult = await _mediator.Send(registerCommand, cancellationToken);

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
    var result = await _mediator.Send(takeTaskCommand, cancellationToken);

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
    CancellationToken cancellationToken)
  {
    // Get user by telegram ID
    var registerCommand = new RegisterUserCommand(chatId, "User");
    var userResult = await _mediator.Send(registerCommand, cancellationToken);

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
    var result = await _mediator.Send(completeTaskCommand, cancellationToken);

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
    CancellationToken cancellationToken)
  {
    await botClient.SendTextMessageAsync(
      chatId,
      "🏠 Действие с семьей\n(В разработке)",
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
}
