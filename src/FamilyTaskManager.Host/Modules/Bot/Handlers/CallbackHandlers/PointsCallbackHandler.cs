using FamilyTaskManager.Core.TaskAggregate;
using FamilyTaskManager.Host.Modules.Bot.Handlers.Commands;
using FamilyTaskManager.Host.Modules.Bot.Handlers.ConversationHandlers;
using FamilyTaskManager.Host.Modules.Bot.Helpers;
using FamilyTaskManager.Host.Modules.Bot.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.CallbackHandlers;

public class PointsCallbackHandler(
  TaskCreationHandler taskCreationHandler,
  TemplateCreationHandler templateCreationHandler,
  TemplateEditHandler templateEditHandler,
  TemplateCommandHandler templateCommandHandler)
{
  public async Task HandlePointsSelectionAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    string[] parts,
    UserSession session,
    CancellationToken cancellationToken)
  {
    if (parts.Length < 2) return;

    var selection = parts[1];

    // Handle back button
    if (selection == "back")
    {
      await HandleBackFromPointsAsync(botClient, chatId, messageId, session, cancellationToken);
      return;
    }

    if (!int.TryParse(selection, out var points)) return;

    if (!TaskPoints.IsValidValue(points)) return;
    var fakeMessage = new Message
    {
      Chat = new() { Id = chatId },
      MessageId = messageId
    };

    if (session.State != ConversationState.AwaitingTemplateEditPoints)
      await botClient.DeleteMessageAsync(chatId, messageId, cancellationToken);

    switch (session.State)
    {
      case ConversationState.AwaitingTaskPoints:
        await taskCreationHandler.HandleTaskPointsInputAsync(
          botClient,
          fakeMessage,
          session,
          points.ToString(),
          cancellationToken);
        break;

      case ConversationState.AwaitingTemplatePoints:
        await templateCreationHandler.HandleTemplatePointsInputAsync(
          botClient,
          fakeMessage,
          session,
          points.ToString(),
          cancellationToken);
        break;

      case ConversationState.AwaitingTemplateEditPoints:
        await templateEditHandler.HandleTemplateEditPointsInputAsync(
          botClient,
          fakeMessage,
          session,
          points.ToString(),
          cancellationToken);
        break;
    }
  }

  private async Task HandleBackFromPointsAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    UserSession session,
    CancellationToken cancellationToken)
  {
    // Determine previous state based on current state
    ConversationState previousState;
    string messageText;
    IReplyMarkup? keyboard;

    switch (session.State)
    {
      case ConversationState.AwaitingTaskPoints:
        // Delete message and send new one for task creation
        await botClient.DeleteMessageAsync(chatId, messageId, cancellationToken);
        previousState = ConversationState.AwaitingTaskTitle;
        messageText = "📝 Введите название задачи (от 3 до 100 символов):";
        keyboard = StateKeyboardHelper.GetKeyboardForState(previousState);
        session.State = previousState;
        await botClient.SendTextMessageAsync(
          chatId,
          messageText + StateKeyboardHelper.GetHintForState(previousState),
          replyMarkup: keyboard,
          cancellationToken: cancellationToken);
        break;

      case ConversationState.AwaitingTemplatePoints:
        // Delete message and send new one for template creation
        await botClient.DeleteMessageAsync(chatId, messageId, cancellationToken);
        previousState = ConversationState.AwaitingTemplateTitle;
        messageText = "📝 Введите название шаблона задачи (от 3 до 100 символов):";
        keyboard = StateKeyboardHelper.GetKeyboardForState(previousState);
        session.State = previousState;
        await botClient.SendTextMessageAsync(
          chatId,
          messageText + StateKeyboardHelper.GetHintForState(previousState),
          replyMarkup: keyboard,
          cancellationToken: cancellationToken);
        break;

      case ConversationState.AwaitingTemplateEditPoints:
        // For edit mode, edit existing message to show template edit screen
        if (session.Data.TemplateId is Guid templateId)
        {
          session.ClearState();
          await templateCommandHandler.HandleEditTemplateAsync(
            botClient,
            chatId,
            messageId,
            templateId,
            session,
            cancellationToken);
        }
        else
        {
          await botClient.DeleteMessageAsync(chatId, messageId, cancellationToken);
          session.ClearState();
          await botClient.SendTextMessageAsync(
            chatId,
            "❌ Ошибка. Попробуйте снова.",
            replyMarkup: MainMenuHelper.GetMainMenuKeyboard(),
            cancellationToken: cancellationToken);
        }

        break;

      default:
        return;
    }
  }
}
