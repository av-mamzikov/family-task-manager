using FamilyTaskManager.Core.SpotAggregate;
using FamilyTaskManager.Host.Modules.Bot.Constants;
using FamilyTaskManager.Host.Modules.Bot.Helpers;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.UseCases.Spots;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.ConversationHandlers;

public class SpotCreationHandler(
  ILogger<SpotCreationHandler> logger,
  IMediator mediator) : BaseConversationHandler(logger, mediator), IConversationHandler
{
  private const string StateAwaitingName = "awaiting_name";

  public async Task HandleMessageAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    CancellationToken cancellationToken)
  {
    var text = message.Text;
    if (string.IsNullOrWhiteSpace(text))
      return;

    if (text is "❌ Отменить" or "/cancel" or "⬅️ Назад")
      return;

    if (session.Data.InternalState == StateAwaitingName)
      await HandleSpotNameInputAsync(botClient, message, session, text, cancellationToken);
  }

  public Task HandleCallbackAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    string[] callbackParts,
    UserSession session,
    User fromUser,
    CancellationToken cancellationToken) => Task.CompletedTask;

  public async Task HandleBackAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    Func<Task> sendMainMenuAction,
    CancellationToken cancellationToken)
  {
    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      "⬅️ Возврат отменён.",
      replyMarkup: new ReplyKeyboardRemove(),
      cancellationToken: cancellationToken);
    await sendMainMenuAction();
    session.ClearState();
  }

  private async Task HandleSpotNameInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string SpotName,
    CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(SpotName) || SpotName.Length < 2 || SpotName.Length > 50)
    {
      var keyboard = GetCancelKeyboard();
      await SendValidationErrorAsync(
        botClient,
        message.Chat.Id,
        "❌ Имя спота должно содержать от 2 до 50 символов. Попробуйте снова:",
        "\n\n💡 Используйте кнопку \"❌ Отменить\" для отмены.",
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
      BotMessages.Messages.SpotTasksAvailable,
      replyMarkup: MainMenuHelper.GetMainMenuKeyboard(),
      cancellationToken: cancellationToken);
    session.ClearState();
  }

  private static ReplyKeyboardMarkup GetCancelKeyboard() =>
    new([[new("❌ Отменить")]])
    {
      ResizeKeyboard = true
    };
}
