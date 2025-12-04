using FamilyTaskManager.Core.SpotAggregate;
using FamilyTaskManager.Host.Modules.Bot.Helpers;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.UseCases.Spots;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.ConversationHandlers;

public class SpotCreationHandler(
  ILogger<SpotCreationHandler> logger,
  IMediator mediator) : BaseConversationHandler(logger, mediator)
{
  public async Task HandleSpotNameInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string SpotName,
    CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(SpotName) || SpotName.Length < 2 || SpotName.Length > 50)
    {
      var keyboard = StateKeyboardHelper.GetKeyboardForState(ConversationState.AwaitingSpotName);
      await SendValidationErrorAsync(
        botClient,
        message.Chat.Id,
        "❌ Имя спота должно содержать от 2 до 50 символов. Попробуйте снова:",
        StateKeyboardHelper.GetHintForState(ConversationState.AwaitingSpotName),
        keyboard,
        cancellationToken);
      return;
    }

    // Get data from session
    if (session.Data.SpotType == null || session.CurrentFamilyId == null)
    {
      await SendErrorAndClearStateAsync(
        botClient,
        message.Chat.Id,
        session,
        "❌ Ошибка. Попробуйте создать спота заново.",
        cancellationToken);
      return;
    }

    // Parse Spot type
    if (!Enum.TryParse<SpotType>(session.Data.SpotType, true, out var SpotType))
    {
      await SendErrorAndClearStateAsync(
        botClient,
        message.Chat.Id,
        session,
        "❌ Ошибка типа спота. Попробуйте создать спота заново.",
        cancellationToken);
      return;
    }

    // Create Spot (spot)
    var createSpotCommand = new CreateSpotCommand(session.CurrentFamilyId.Value, SpotType, SpotName);
    var result = await Mediator.Send(createSpotCommand, cancellationToken);

    if (!result.IsSuccess)
    {
      await SendErrorAndClearStateAsync(
        botClient,
        message.Chat.Id,
        session,
        $"❌ Ошибка создания спота: {result.Errors.FirstOrDefault()}",
        cancellationToken);
      return;
    }

    var SpotEmoji = SpotTypeHelper.GetEmoji(SpotType);

    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      $"✅ Спот {SpotEmoji} \"{SpotName}\" успешно создан!\n\n" +
      BotConstants.Messages.SpotTasksAvailable,
      replyMarkup: MainMenuHelper.GetMainMenuKeyboard(),
      cancellationToken: cancellationToken);
    session.ClearState();
  }
}
