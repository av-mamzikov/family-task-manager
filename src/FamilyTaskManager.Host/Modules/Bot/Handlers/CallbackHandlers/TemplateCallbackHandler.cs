using FamilyTaskManager.Core.PetAggregate;
using FamilyTaskManager.Host.Modules.Bot.Handlers.Commands;
using FamilyTaskManager.Host.Modules.Bot.Helpers;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.Host.Modules.Bot.Services;
using FamilyTaskManager.UseCases.Pets;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.CallbackHandlers;

public class TemplateCallbackHandler(
  ILogger<TemplateCallbackHandler> logger,
  IMediator mediator,
  IUserRegistrationService userRegistrationService,
  TemplateCommandHandler templateCommandHandler)
  : BaseCallbackHandler(logger, mediator, userRegistrationService)
{
  public async Task HandleTemplateActionAsync(
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

    var templateAction = parts[1];

    switch (templateAction)
    {
      case "vp" when parts.Length >= 3 && Guid.TryParse(parts[2], out var petId):
        await templateCommandHandler.HandleViewPetTemplatesAsync(botClient, chatId, messageId, petId, session,
          cancellationToken);
        break;

      case "v" when parts.Length >= 3 && Guid.TryParse(parts[2], out var templateId):
        await templateCommandHandler.HandleViewTemplateAsync(botClient, chatId, messageId, templateId, session,
          cancellationToken);
        break;

      case "d" when parts.Length >= 3 && Guid.TryParse(parts[2], out var templateId):
        await templateCommandHandler.HandleDeleteTemplateAsync(botClient, chatId, messageId, templateId, session,
          cancellationToken);
        break;

      case "cd" when parts.Length >= 3 && Guid.TryParse(parts[2], out var templateId):
        await templateCommandHandler.HandleConfirmDeleteTemplateAsync(botClient, chatId, messageId, templateId,
          session, cancellationToken);
        break;

      case "e" when parts.Length >= 3 && Guid.TryParse(parts[2], out var templateId):
        await templateCommandHandler.HandleEditTemplateAsync(botClient, chatId, messageId, templateId, session,
          cancellationToken);
        break;

      case "ef" when parts.Length >= 4 && Guid.TryParse(parts[2], out var templateId):
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

      case "c":
        await HandleTemplateCreateAsync(botClient, chatId, messageId, session, fromUser, cancellationToken);
        break;

      case "cf" when parts.Length >= 3 && Guid.TryParse(parts[2], out var petId):
        await HandleTemplateCreateForPetAsync(botClient, chatId, messageId, petId, session, fromUser,
          cancellationToken);
        break;

      case "ct" when parts.Length >= 3 && Guid.TryParse(parts[2], out var templateId):
        await templateCommandHandler.HandleCreateTaskNowAsync(botClient, chatId, messageId, templateId, session,
          cancellationToken);
        break;

      case "b":
        // Re-show templates menu
        var userId = await GetOrRegisterUserAsync(fromUser, cancellationToken);
        if (userId != null)
        {
          var message = new Message { Chat = new Chat { Id = chatId } };
          await templateCommandHandler.HandleAsync(botClient, message, session, userId.Value, cancellationToken);
        }

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
      await SendErrorAsync(botClient, chatId, BotConstants.Errors.NoFamily, cancellationToken);
      return;
    }

    // Store template ID and family ID in session
    session.Data["templateId"] = templateId;
    session.Data["familyId"] = session.CurrentFamilyId.Value;

    switch (field)
    {
      case "title":
        session.State = ConversationState.AwaitingTemplateEditTitle;
        await botClient.EditMessageTextAsync(
          chatId,
          messageId,
          "✏️ Введите новое название шаблона (от 3 до 100 символов):",
          cancellationToken: cancellationToken);
        break;

      case "points":
        session.State = ConversationState.AwaitingTemplateEditPoints;
        var pointsKeyboard = TaskPointsHelper.GetPointsSelectionKeyboard();
        await botClient.EditMessageTextAsync(
          chatId,
          messageId,
          "⭐ Выберите новую сложность задачи:",
          replyMarkup: pointsKeyboard,
          cancellationToken: cancellationToken);
        break;

      case "schedule":
        session.State = ConversationState.AwaitingTemplateEditScheduleType;
        var scheduleTypeKeyboard = ScheduleKeyboardHelper.GetScheduleTypeKeyboard();
        await botClient.EditMessageTextAsync(
          chatId,
          messageId,
          BotConstants.Templates.ChooseScheduleType +
          StateKeyboardHelper.GetHintForState(ConversationState.AwaitingTemplateEditScheduleType),
          replyMarkup: scheduleTypeKeyboard,
          cancellationToken: cancellationToken);
        break;

      case "dueduration":
        session.State = ConversationState.AwaitingTemplateEditDueDuration;
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
      await SendErrorAsync(botClient, chatId, BotConstants.Errors.NoFamily, cancellationToken);
      return;
    }

    // Get pets for the family
    var getPetsQuery = new GetPetsQuery(session.CurrentFamilyId.Value);
    var petsResult = await Mediator.Send(getPetsQuery, cancellationToken);

    if (!petsResult.IsSuccess || !petsResult.Value.Any())
    {
      await EditMessageWithErrorAsync(botClient, chatId, messageId, BotConstants.Errors.NoPets, cancellationToken);
      return;
    }

    // Build pet selection keyboard
    var buttons = petsResult.Value.Select(p =>
    {
      var petEmoji = p.Type switch
      {
        PetType.Cat => "🐱",
        PetType.Dog => "🐶",
        PetType.Hamster => "🐹",
        _ => "🐾"
      };
      return new[] { InlineKeyboardButton.WithCallbackData($"{petEmoji} {p.Name}", $"tpl_cf_{p.Id}") };
    }).ToArray();

    var keyboard = new InlineKeyboardMarkup(buttons);

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      "🐾 Выберите питомца для создания шаблона:",
      replyMarkup: keyboard,
      cancellationToken: cancellationToken);
  }

  private async Task HandleTemplateCreateForPetAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    Guid petId,
    UserSession session,
    User fromUser,
    CancellationToken cancellationToken)
  {
    if (session.CurrentFamilyId == null)
    {
      await SendErrorAsync(botClient, chatId, BotConstants.Errors.NoFamily, cancellationToken);
      return;
    }

    // Store pet ID and family ID in session
    session.SetState(ConversationState.AwaitingTemplateTitle,
      new Dictionary<string, object> { ["petId"] = petId, ["familyId"] = session.CurrentFamilyId.Value });

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      BotConstants.Templates.EnterTemplateTitle,
      cancellationToken: cancellationToken);
  }
}
