using FamilyTaskManager.Host.Modules.Bot.Helpers;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.Host.Modules.Bot.Services;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.CallbackHandlers;

public class ScheduleCallbackHandler(
  ILogger<ScheduleCallbackHandler> logger,
  IMediator mediator,
  IUserRegistrationService userRegistrationService)
  : BaseCallbackHandler(logger, mediator, userRegistrationService)
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
      case "type":
        await HandleScheduleTypeSelectionAsync(botClient, chatId, messageId, value, session, cancellationToken);
        break;

      case "weekday":
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
    // Store schedule type in session
    session.Data["scheduleType"] = scheduleType;

    // Delete the inline keyboard message
    await botClient.DeleteMessageAsync(chatId, messageId, cancellationToken);

    // For Manual schedule type, skip time input but ask for DueDuration
    IReplyMarkup? keyboard;
    if (scheduleType == "manual")
    {
      // Determine if we're in template creation or editing flow
      var isEditing = session.State == ConversationState.AwaitingTemplateEditScheduleType;
      var isTask = session.State == ConversationState.AwaitingTaskScheduleType;

      await botClient.SendTextMessageAsync(
        chatId,
        "✅ Выбран тип расписания: Вручную",
        cancellationToken: cancellationToken);

      // For editing, ask for DueDuration
      if (isEditing)
      {
        session.State = ConversationState.AwaitingTemplateEditDueDuration;
        keyboard = StateKeyboardHelper.GetKeyboardForState(ConversationState.AwaitingTemplateEditDueDuration);
        await botClient.SendTextMessageAsync(
          chatId,
          BotConstants.Templates.EnterDueDuration +
          StateKeyboardHelper.GetHintForState(ConversationState.AwaitingTemplateEditDueDuration),
          replyMarkup: keyboard,
          cancellationToken: cancellationToken);
      }
      else if (!isTask)
      {
        // For template creation, ask for DueDuration
        session.State = ConversationState.AwaitingTemplateDueDuration;
        keyboard = StateKeyboardHelper.GetKeyboardForState(ConversationState.AwaitingTemplateDueDuration);
        await botClient.SendTextMessageAsync(
          chatId,
          BotConstants.Templates.EnterDueDuration +
          StateKeyboardHelper.GetHintForState(ConversationState.AwaitingTemplateDueDuration),
          replyMarkup: keyboard,
          cancellationToken: cancellationToken);
      }
      else
        // For task creation flows, keep state for now (will be implemented later)
        session.State = ConversationState.None;

      return;
    }

    // Determine next state based on current conversation state
    var isEditingFlow = session.State == ConversationState.AwaitingTemplateEditScheduleType;
    var isTaskFlow = session.State == ConversationState.AwaitingTaskScheduleType;

    ConversationState nextState;
    if (isEditingFlow)
      nextState = ConversationState.AwaitingTemplateEditScheduleTime;
    else if (isTaskFlow)
      nextState = ConversationState.AwaitingTaskScheduleTime;
    else
      nextState = ConversationState.AwaitingTemplateScheduleTime;

    session.State = nextState;

    // Ask for time
    keyboard = StateKeyboardHelper.GetKeyboardForState(nextState);
    await botClient.SendTextMessageAsync(
      chatId,
      BotConstants.Templates.EnterScheduleTime +
      StateKeyboardHelper.GetHintForState(nextState),
      replyMarkup: keyboard,
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
    session.Data["scheduleDayOfWeek"] = dayOfWeek.Value;

    // Delete the inline keyboard message
    await botClient.DeleteMessageAsync(chatId, messageId, cancellationToken);

    // Determine if we're in template creation or editing flow
    var isEditing = session.State == ConversationState.AwaitingTemplateEditScheduleWeekday;
    var isTask = session.State == ConversationState.AwaitingTaskScheduleWeekday;

    // Send confirmation message
    var weekdayName = ScheduleKeyboardHelper.GetWeekdayName(dayOfWeek.Value);
    await botClient.SendTextMessageAsync(
      chatId,
      $"✅ Выбран день недели: {weekdayName}",
      cancellationToken: cancellationToken);

    // For editing, ask for DueDuration
    if (isEditing)
    {
      session.State = ConversationState.AwaitingTemplateEditDueDuration;
      var keyboard = StateKeyboardHelper.GetKeyboardForState(ConversationState.AwaitingTemplateEditDueDuration);
      await botClient.SendTextMessageAsync(
        chatId,
        BotConstants.Templates.EnterDueDuration +
        StateKeyboardHelper.GetHintForState(ConversationState.AwaitingTemplateEditDueDuration),
        replyMarkup: keyboard,
        cancellationToken: cancellationToken);
    }
    else if (!isTask)
    {
      // For template creation, ask for DueDuration
      session.State = ConversationState.AwaitingTemplateDueDuration;
      var keyboard = StateKeyboardHelper.GetKeyboardForState(ConversationState.AwaitingTemplateDueDuration);
      await botClient.SendTextMessageAsync(
        chatId,
        BotConstants.Templates.EnterDueDuration +
        StateKeyboardHelper.GetHintForState(ConversationState.AwaitingTemplateDueDuration),
        replyMarkup: keyboard,
        cancellationToken: cancellationToken);
    }
    else
      // For task creation flows, keep state for now (will be implemented later)
      session.State = ConversationState.None;
  }
}
