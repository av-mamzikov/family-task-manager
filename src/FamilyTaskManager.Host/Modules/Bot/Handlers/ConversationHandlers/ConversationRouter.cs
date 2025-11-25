using FamilyTaskManager.Host.Modules.Bot.Helpers;
using FamilyTaskManager.Host.Modules.Bot.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.ConversationHandlers;

public interface IConversationRouter
{
  Task HandleConversationAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    CancellationToken cancellationToken);

  Task HandleCancelConversationAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    Func<Task> sendMainMenuAction,
    CancellationToken cancellationToken);

  Task HandleBackInConversationAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    Func<Task> sendMainMenuAction,
    CancellationToken cancellationToken);
}

public class ConversationRouter(
  FamilyCreationHandler familyCreationHandler,
  PetCreationHandler petCreationHandler,
  TaskCreationHandler taskCreationHandler,
  TemplateCreationHandler templateCreationHandler,
  TemplateEditHandler templateEditHandler)
  : IConversationRouter
{
  public async Task HandleConversationAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    CancellationToken cancellationToken)
  {
    // Handle location messages
    if (message.Location != null && session.State == ConversationState.AwaitingFamilyLocation)
    {
      await familyCreationHandler.HandleFamilyLocationInputAsync(botClient, message, session, cancellationToken);
      return;
    }

    var text = message.Text!;

    // Handle universal commands
    if (text is "❌ Отменить" or "/cancel" or "⬅️ Назад")
    {
      // These are handled by CommandHandler
      return;
    }

    // Route to appropriate handler based on state
    await (session.State switch
    {
      ConversationState.AwaitingFamilyName =>
        familyCreationHandler.HandleFamilyNameInputAsync(botClient, message, session, text, cancellationToken),

      ConversationState.AwaitingFamilyTimezone =>
        HandleTimezoneTextInput(botClient, message, cancellationToken),

      ConversationState.AwaitingFamilyLocation =>
        HandleLocationTextInput(botClient, message, session, cancellationToken),

      ConversationState.AwaitingPetName =>
        petCreationHandler.HandlePetNameInputAsync(botClient, message, session, text, cancellationToken),

      ConversationState.AwaitingTaskTitle =>
        taskCreationHandler.HandleTaskTitleInputAsync(botClient, message, session, text, cancellationToken),

      ConversationState.AwaitingTaskPoints =>
        taskCreationHandler.HandleTaskPointsInputAsync(botClient, message, session, text, cancellationToken),

      ConversationState.AwaitingTaskDueDate =>
        taskCreationHandler.HandleTaskDueDateInputAsync(botClient, message, session, text, cancellationToken),

      ConversationState.AwaitingTaskSchedule =>
        taskCreationHandler.HandleTaskScheduleInputAsync(botClient, message, session, text, cancellationToken),

      ConversationState.AwaitingTemplateTitle =>
        templateCreationHandler.HandleTemplateTitleInputAsync(botClient, message, session, text, cancellationToken),

      ConversationState.AwaitingTemplatePoints =>
        templateCreationHandler.HandleTemplatePointsInputAsync(botClient, message, session, text, cancellationToken),

      ConversationState.AwaitingTemplateSchedule =>
        templateCreationHandler.HandleTemplateScheduleInputAsync(botClient, message, session, text, cancellationToken),

      ConversationState.AwaitingTemplateEditTitle =>
        templateEditHandler.HandleTemplateEditTitleInputAsync(botClient, message, session, text, cancellationToken),

      ConversationState.AwaitingTemplateEditPoints =>
        templateEditHandler.HandleTemplateEditPointsInputAsync(botClient, message, session, text, cancellationToken),

      ConversationState.AwaitingTemplateEditSchedule =>
        templateEditHandler.HandleTemplateEditScheduleInputAsync(botClient, message, session, text, cancellationToken),

      _ => HandleUnknownState(botClient, message, session, cancellationToken)
    });
  }

  public async Task HandleCancelConversationAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    Func<Task> sendMainMenuAction,
    CancellationToken cancellationToken)
  {
    session.ClearState();

    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      "❌ Действие отменено.",
      replyMarkup: new ReplyKeyboardRemove(),
      cancellationToken: cancellationToken);

    // Return to main menu
    await sendMainMenuAction();
  }

  public async Task HandleBackInConversationAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    Func<Task> sendMainMenuAction,
    CancellationToken cancellationToken)
  {
    var currentState = session.State;

    // Determine previous state based on current state
    var (previousState, shouldClear) = currentState switch
    {
      // Task creation flow
      ConversationState.AwaitingTaskPoints => (ConversationState.AwaitingTaskTitle, false),
      ConversationState.AwaitingTaskPetSelection => (ConversationState.AwaitingTaskPoints, false),
      ConversationState.AwaitingTaskSchedule => (ConversationState.AwaitingTaskPetSelection, false),
      ConversationState.AwaitingTaskDueDate => (ConversationState.AwaitingTaskPetSelection, false),

      // Template creation flow
      ConversationState.AwaitingTemplatePoints => (ConversationState.AwaitingTemplateTitle, false),
      ConversationState.AwaitingTemplatePetSelection => (ConversationState.AwaitingTemplatePoints, false),
      ConversationState.AwaitingTemplateSchedule => (ConversationState.AwaitingTemplatePoints, false),

      // Template editing flow
      ConversationState.AwaitingTemplateEditTitle => (ConversationState.None, true),
      ConversationState.AwaitingTemplateEditPoints => (ConversationState.None, true),
      ConversationState.AwaitingTemplateEditSchedule => (ConversationState.None, true),

      // Family creation flow
      ConversationState.AwaitingFamilyLocation => (ConversationState.AwaitingFamilyTimezone, false),

      _ => (ConversationState.None, true)
    };

    if (shouldClear)
    {
      session.ClearState();
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        "⬅️ Возврат отменён.",
        replyMarkup: new ReplyKeyboardRemove(),
        cancellationToken: cancellationToken);
      await sendMainMenuAction();
      return;
    }

    // Set previous state
    session.State = previousState;

    // Send appropriate message for the previous state
    var keyboard = StateKeyboardHelper.GetKeyboardForState(previousState);
    var hint = StateKeyboardHelper.GetHintForState(previousState);

    var messageText = previousState switch
    {
      ConversationState.AwaitingTaskTitle => "📝 Введите название задачи (от 3 до 100 символов):" + hint,
      ConversationState.AwaitingTaskPoints => "💯 Введите количество очков за выполнение задачи (от 1 до 100):" + hint,
      ConversationState.AwaitingTemplateTitle => "📝 Введите название шаблона (от 3 до 100 символов):" + hint,
      ConversationState.AwaitingTemplatePoints => "💯 Введите количество очков (от 1 до 100):" + hint,
      ConversationState.AwaitingFamilyTimezone => "🌍 Выберите способ определения временной зоны:",
      _ => "⬅️ Возврат к предыдущему шагу."
    };

    if (previousState == ConversationState.AwaitingFamilyTimezone)
    {
      var timezoneKeyboard = GetTimezoneChoiceKeyboard();
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        messageText,
        replyMarkup: timezoneKeyboard,
        cancellationToken: cancellationToken);
    }
    else
    {
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        messageText,
        replyMarkup: keyboard ?? new ReplyKeyboardRemove(),
        cancellationToken: cancellationToken);
    }
  }

  private static async Task HandleTimezoneTextInput(
    ITelegramBotClient botClient,
    Message message,
    CancellationToken cancellationToken) =>
    // Timezone selection is handled via callbacks, not text input
    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      "❌ Пожалуйста, используйте кнопки для выбора временной зоны.",
      cancellationToken: cancellationToken);

  private async Task HandleLocationTextInput(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    CancellationToken cancellationToken)
  {
    // Handle "Back" button
    if (message.Text == "⬅️ Назад")
    {
      await familyCreationHandler.HandleBackToTimezoneSelectionAsync(botClient, message, session, cancellationToken);
      return;
    }

    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      "❌ Пожалуйста, используйте кнопку \"📍 Отправить местоположение\" для определения временной зоны.",
      cancellationToken: cancellationToken);
  }

  private static async Task HandleUnknownState(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    CancellationToken cancellationToken)
  {
    session.ClearState();
    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      "❌ Произошла ошибка. Попробуйте снова.",
      cancellationToken: cancellationToken);
  }

  private static InlineKeyboardMarkup GetTimezoneChoiceKeyboard() =>
    new(new[]
    {
      new[] { InlineKeyboardButton.WithCallbackData("📍 Определить по геолокации", "timezone_detect") },
      new[] { InlineKeyboardButton.WithCallbackData("📋 Выбрать из списка", "timezone_showlist") }
    });
}
