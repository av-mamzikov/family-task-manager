using FamilyTaskManager.Host.Modules.Bot.Constants;
using FamilyTaskManager.Host.Modules.Bot.Helpers;
using FamilyTaskManager.Host.Modules.Bot.Models;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.CallbackHandlers;

public class ScheduleCallbackHandler(
  ILogger<ScheduleCallbackHandler> logger,
  IMediator mediator)
  : BaseCallbackHandler(logger, mediator)
{
  /// <summary>
  ///   Handles schedule-related callbacks (type selection, weekday selection).
  /// </summary>
  public async Task HandleScheduleCallbackAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    string[] parts,
    UserSession session,
    CancellationToken cancellationToken)
  {
    if (parts.Length < 3)
    {
      await EditMessageWithErrorAsync(botClient, chatId, messageId, "❌ Неверный формат callback", cancellationToken);
      return;
    }

    var action = parts[1]; // "type" or "weekday"
    var value = parts[2]; // schedule type or weekday value

    switch (action)
    {
      case var _ when action == CallbackActions.Type:
        await HandleScheduleTypeSelectionAsync(botClient, chatId, messageId, value, session, cancellationToken);
        break;

      case var _ when action == CallbackActions.Weekday:
        await HandleWeekdaySelectionAsync(botClient, chatId, messageId, value, session, cancellationToken);
        break;

      default:
        await EditMessageWithErrorAsync(botClient, chatId, messageId, "❌ Неизвестное действие", cancellationToken);
        break;
    }
  }

  private async Task HandleScheduleTypeSelectionAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    string scheduleType,
    UserSession session,
    CancellationToken cancellationToken)
  {
    // Handle back button
    if (scheduleType == CallbackActions.Back)
    {
      await HandleBackFromScheduleTypeAsync(botClient, chatId, messageId, session, cancellationToken);
      return;
    }

    // Store schedule type in session
    session.Data.ScheduleType = scheduleType;

    // Determine if we're in template creation or editing flow
    var isEditing = session.State == ConversationState.AwaitingTemplateEditScheduleType;
    var isTask = session.State == ConversationState.AwaitingTaskScheduleType;

    // For Manual schedule type, skip time input but ask for DueDuration
    if (scheduleType == "manual")
    {
      ConversationState nextState;
      if (isEditing)
        nextState = ConversationState.AwaitingTemplateEditDueDuration;
      else if (!isTask)
        nextState = ConversationState.AwaitingTemplateDueDuration;
      else
      {
        // For task creation flows, keep state for now (will be implemented later)
        session.State = ConversationState.None;
        await botClient.EditMessageTextAsync(
          chatId,
          messageId,
          "✅ Тип расписания: Вручную",
          cancellationToken: cancellationToken);
        return;
      }

      session.State = nextState;
      var keyboard = StateKeyboardHelper.GetKeyboardForState(nextState) as InlineKeyboardMarkup;
      await botClient.EditMessageTextAsync(
        chatId,
        messageId,
        BotMessages.Templates.EnterDueDuration,
        replyMarkup: keyboard,
        cancellationToken: cancellationToken);
      return;
    }

    // Determine next state based on current conversation state
    ConversationState nextStateForTime;
    if (isEditing)
      nextStateForTime = ConversationState.AwaitingTemplateEditScheduleTime;
    else if (isTask)
      nextStateForTime = ConversationState.AwaitingTaskScheduleTime;
    else
      nextStateForTime = ConversationState.AwaitingTemplateScheduleTime;

    session.State = nextStateForTime;

    // Ask for time
    var timeKeyboard = StateKeyboardHelper.GetKeyboardForState(nextStateForTime) as InlineKeyboardMarkup;
    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      BotMessages.Templates.EnterScheduleTime,
      replyMarkup: timeKeyboard,
      cancellationToken: cancellationToken);
  }

  private async Task HandleWeekdaySelectionAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    string weekdayValue,
    UserSession session,
    CancellationToken cancellationToken)
  {
    var dayOfWeek = ScheduleKeyboardHelper.ParseWeekdayCallback(weekdayValue);

    if (!dayOfWeek.HasValue)
    {
      await EditMessageWithErrorAsync(botClient, chatId, messageId, "❌ Неверный день недели", cancellationToken);
      return;
    }

    // Store weekday in session
    session.Data.ScheduleDayOfWeek = dayOfWeek.Value;

    // Determine if we're in template creation or editing flow
    var isEditing = session.State == ConversationState.AwaitingTemplateEditScheduleWeekday;
    var isTask = session.State == ConversationState.AwaitingTaskScheduleWeekday;

    ConversationState nextState;
    if (isEditing)
      nextState = ConversationState.AwaitingTemplateEditDueDuration;
    else if (!isTask)
      nextState = ConversationState.AwaitingTemplateDueDuration;
    else
    {
      // For task creation flows, keep state for now (will be implemented later)
      session.State = ConversationState.None;
      var weekdayName = ScheduleKeyboardHelper.GetWeekdayName(dayOfWeek.Value);
      await botClient.EditMessageTextAsync(
        chatId,
        messageId,
        $"✅ День недели: {weekdayName}",
        cancellationToken: cancellationToken);
      return;
    }

    session.State = nextState;
    var keyboard = StateKeyboardHelper.GetKeyboardForState(nextState) as InlineKeyboardMarkup;
    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      BotMessages.Templates.EnterDueDuration,
      replyMarkup: keyboard,
      cancellationToken: cancellationToken);
  }

  private async Task HandleBackFromScheduleTypeAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    UserSession session,
    CancellationToken cancellationToken)
  {
    // Determine previous state based on current state
    var isEditing = session.State == ConversationState.AwaitingTemplateEditScheduleType;
    var isTask = session.State == ConversationState.AwaitingTaskScheduleType;

    ConversationState previousState;
    string messageText;

    if (isEditing)
    {
      previousState = ConversationState.AwaitingTemplateEditPoints;
      messageText = "⭐ Выберите сложность задачи:";
    }
    else if (isTask)
    {
      previousState = ConversationState.AwaitingTaskPoints;
      messageText = "⭐ Выберите сложность задачи:";
    }
    else
    {
      previousState = ConversationState.AwaitingTemplatePoints;
      messageText = BotMessages.Templates.EnterTemplatePoints;
    }

    session.State = previousState;

    var keyboard = TaskPointsHelper.GetPointsSelectionKeyboard();
    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      messageText,
      replyMarkup: keyboard,
      cancellationToken: cancellationToken);
  }
}
