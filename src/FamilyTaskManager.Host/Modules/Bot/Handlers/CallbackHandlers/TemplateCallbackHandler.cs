using FamilyTaskManager.Core.SpotAggregate;
using FamilyTaskManager.Host.Modules.Bot.Constants;
using FamilyTaskManager.Host.Modules.Bot.Handlers.Commands;
using FamilyTaskManager.Host.Modules.Bot.Helpers;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.UseCases.Spots;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.CallbackHandlers;

public class TemplateCallbackHandler(
  ILogger<TemplateCallbackHandler> logger,
  IMediator mediator,
  TemplateCommandHandler templateCommandHandler)
  : BaseCallbackHandler(logger, mediator), ICallbackHandler
{
  public async Task Handle(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    string[] parts,
    UserSession session,
    User fromUser,
    CancellationToken cancellationToken) =>
    await HandleTemplateActionAsync(botClient, chatId, messageId, parts, session, fromUser, cancellationToken);

  public async Task HandleTemplateActionAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    string[] parts,
    UserSession session,
    User fromUser,
    CancellationToken cancellationToken)
  {
    if (parts.Length < 2) return;

    var templateAction = parts[1];

    switch (templateAction)
    {
      case var _ when templateAction == CallbackActions.ViewForSpot && parts.Length >= 3 &&
                      Guid.TryParse(parts[2], out var SpotId):
        await templateCommandHandler.HandleViewSpotTemplatesAsync(botClient, chatId, messageId, SpotId, session,
          cancellationToken);
        break;

      case var _ when templateAction == CallbackActions.View && parts.Length >= 3 &&
                      Guid.TryParse(parts[2], out var templateId):
        await templateCommandHandler.HandleViewTemplateAsync(botClient, chatId, messageId, templateId, session,
          cancellationToken);
        break;

      case var _ when templateAction == CallbackActions.Delete && parts.Length >= 3 &&
                      Guid.TryParse(parts[2], out var templateId):
        await templateCommandHandler.HandleDeleteTemplateAsync(botClient, chatId, messageId, templateId, session,
          cancellationToken);
        break;

      case var _ when templateAction == CallbackActions.ConfirmDelete && parts.Length >= 3 &&
                      Guid.TryParse(parts[2], out var templateId):
        await templateCommandHandler.HandleConfirmDeleteTemplateAsync(botClient, chatId, messageId, templateId,
          session, cancellationToken);
        break;

      case var _ when templateAction == CallbackActions.Edit && parts.Length >= 3 &&
                      Guid.TryParse(parts[2], out var templateId):
        await templateCommandHandler.HandleEditTemplateAsync(botClient, chatId, messageId, templateId, session,
          cancellationToken);
        break;

      case var _ when templateAction == CallbackActions.EditField && parts.Length >= 4 &&
                      Guid.TryParse(parts[2], out var templateId):
        var fieldMap = new Dictionary<string, string>
        {
          { "t", "title" },
          { "p", "points" },
          { "s", "schedule" },
          { "d", "dueduration" }
        };
        var fieldName = fieldMap.GetValueOrDefault(parts[3], "title");
        await HandleTemplateEditFieldAsync(botClient, chatId, messageId, templateId, fieldName, session,
          cancellationToken);
        break;

      case var _ when templateAction == CallbackActions.Create:
        await HandleTemplateCreateAsync(botClient, chatId, messageId, session, fromUser, cancellationToken);
        break;

      case var _ when templateAction == CallbackActions.CreateForSpot && parts.Length >= 3 &&
                      Guid.TryParse(parts[2], out var SpotId):
        await HandleTemplateCreateForSpotAsync(botClient, chatId, messageId, SpotId, session, fromUser,
          cancellationToken);
        break;

      case var _ when templateAction == CallbackActions.CreateTask && parts.Length >= 3 &&
                      Guid.TryParse(parts[2], out var templateId):
        await templateCommandHandler.HandleCreateTaskNowAsync(botClient, chatId, messageId, templateId, session,
          cancellationToken);
        break;

      case var _ when templateAction == CallbackActions.Back:
        // Re-show templates menu
        var message = new Message { Chat = new() { Id = chatId } };
        await templateCommandHandler.HandleAsync(botClient, message, session, session.UserId, cancellationToken);

        break;

      default:
        await SendErrorAsync(botClient, chatId, "❌ Неизвестное действие", cancellationToken);
        break;
    }
  }

  private async Task HandleTemplateEditFieldAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    Guid templateId,
    string field,
    UserSession session,
    CancellationToken cancellationToken)
  {
    if (session.CurrentFamilyId == null)
    {
      await SendErrorAsync(botClient, chatId, BotMessages.Errors.NoFamily, cancellationToken);
      return;
    }

    session.Data.TemplateId = templateId;

    switch (field)
    {
      case "title":
        session.State = ConversationState.TemplateEdit;
        session.Data.InternalState = "awaiting_title";
        await botClient.EditMessageTextAsync(
          chatId,
          messageId,
          "✏️ Введите новое название шаблона (от 3 до 100 символов):",
          cancellationToken: cancellationToken);
        break;

      case "points":
        session.State = ConversationState.TemplateEdit;
        session.Data.InternalState = "awaiting_points";
        var pointsKeyboard = TaskPointsHelper.GetPointsSelectionKeyboard();
        await botClient.EditMessageTextAsync(
          chatId,
          messageId,
          "⭐ Выберите новую сложность задачи:",
          replyMarkup: pointsKeyboard,
          cancellationToken: cancellationToken);
        break;

      case "schedule":
        session.State = ConversationState.TemplateEdit;
        session.Data.InternalState = "awaiting_schedule_type";
        var scheduleTypeKeyboard = ScheduleKeyboardHelper.GetScheduleTypeKeyboard();
        await botClient.EditMessageTextAsync(
          chatId,
          messageId,
          BotMessages.Templates.ChooseScheduleType +
          "\n\n💡 Используйте кнопки для выбора.",
          replyMarkup: scheduleTypeKeyboard,
          cancellationToken: cancellationToken);
        break;

      case "dueduration":
        session.State = ConversationState.TemplateEdit;
        session.Data.InternalState = "awaiting_due_duration";
        await botClient.EditMessageTextAsync(
          chatId,
          messageId,
          "⏰ Введите новый срок выполнения в часах (от 0 до 24):",
          cancellationToken: cancellationToken);
        break;

      default:
        await SendErrorAsync(botClient, chatId, "❌ Неизвестное поле", cancellationToken);
        break;
    }
  }

  private async Task HandleTemplateCreateAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    UserSession session,
    User fromUser,
    CancellationToken cancellationToken)
  {
    if (session.CurrentFamilyId == null)
    {
      await SendErrorAsync(botClient, chatId, BotMessages.Errors.NoFamily, cancellationToken);
      return;
    }

    // Get Spots for the family
    var getSpotsQuery = new GetSpotsQuery(session.CurrentFamilyId.Value);
    var SpotsResult = await Mediator.Send(getSpotsQuery, cancellationToken);

    if (!SpotsResult.IsSuccess || !SpotsResult.Value.Any())
    {
      await EditMessageWithErrorAsync(botClient, chatId, messageId, BotMessages.Errors.NoSpots, cancellationToken);
      return;
    }

    // Build Spot selection keyboard
    var buttons = SpotsResult.Value.Select(p =>
    {
      var SpotEmoji = p.Type switch
      {
        SpotType.Cat => "🐱",
        SpotType.Dog => "🐶",
        SpotType.Hamster => "🐹",
        _ => "🐾"
      };
      return new[]
        { InlineKeyboardButton.WithCallbackData($"{SpotEmoji} {p.Name}", CallbackData.Templates.CreateForSpot(p.Id)) };
    }).ToArray();

    var keyboard = new InlineKeyboardMarkup(buttons);

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      "🐾 Выберите спота для создания шаблона:",
      replyMarkup: keyboard,
      cancellationToken: cancellationToken);
  }

  private async Task HandleTemplateCreateForSpotAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    Guid SpotId,
    UserSession session,
    User fromUser,
    CancellationToken cancellationToken)
  {
    if (session.CurrentFamilyId == null)
    {
      await SendErrorAsync(botClient, chatId, BotMessages.Errors.NoFamily, cancellationToken);
      return;
    }

    session.State = ConversationState.TemplateCreation;
    session.Data = new() { SpotId = SpotId, InternalState = "awaiting_title" };

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      BotMessages.Templates.EnterTemplateTitle,
      cancellationToken: cancellationToken);
  }
}
