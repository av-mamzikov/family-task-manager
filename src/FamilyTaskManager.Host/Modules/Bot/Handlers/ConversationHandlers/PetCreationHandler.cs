using FamilyTaskManager.Core.PetAggregate;
using FamilyTaskManager.Host.Modules.Bot.Helpers;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.UseCases.Pets;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.ConversationHandlers;

public class PetCreationHandler(
  ILogger<PetCreationHandler> logger,
  IMediator mediator) : BaseConversationHandler(logger, mediator)
{
  public async Task HandlePetNameInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string petName,
    CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(petName) || petName.Length < 2 || petName.Length > 50)
    {
      var keyboard = StateKeyboardHelper.GetKeyboardForState(ConversationState.AwaitingPetName);
      await SendValidationErrorAsync(
        botClient,
        message.Chat.Id,
        "❌ Имя питомца должно содержать от 2 до 50 символов. Попробуйте снова:",
        StateKeyboardHelper.GetHintForState(ConversationState.AwaitingPetName),
        keyboard,
        cancellationToken);
      return;
    }

    // Get data from session
    if (!TryGetSessionData<string>(session, "petType", out var petTypeStr) ||
        !TryGetSessionData<Guid>(session, "familyId", out var familyId))
    {
      await SendErrorAndClearStateAsync(
        botClient,
        message.Chat.Id,
        session,
        "❌ Ошибка. Попробуйте создать питомца заново.",
        cancellationToken);
      return;
    }

    // Parse pet type
    if (!Enum.TryParse<PetType>(petTypeStr, true, out var petType))
    {
      await SendErrorAndClearStateAsync(
        botClient,
        message.Chat.Id,
        session,
        "❌ Ошибка типа питомца. Попробуйте создать питомца заново.",
        cancellationToken);
      return;
    }

    // Create pet
    var createPetCommand = new CreatePetCommand(familyId, petType, petName);
    var result = await Mediator.Send(createPetCommand, cancellationToken);

    if (!result.IsSuccess)
    {
      await SendErrorAndClearStateAsync(
        botClient,
        message.Chat.Id,
        session,
        $"❌ Ошибка создания питомца: {result.Errors.FirstOrDefault()}",
        cancellationToken);
      return;
    }

    session.ClearState();

    var petEmoji = petType switch
    {
      PetType.Cat => "🐱",
      PetType.Dog => "🐶",
      PetType.Hamster => "🐹",
      _ => "🐾"
    };

    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      $"✅ Питомец {petEmoji} \"{petName}\" успешно создан!\n\n" +
      BotConstants.Messages.PetTasksAvailable,
      replyMarkup: MainMenuHelper.GetMainMenuKeyboard(),
      cancellationToken: cancellationToken);
  }
}
