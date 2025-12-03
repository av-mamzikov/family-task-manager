using FamilyTaskManager.Core.PetAggregate;
using FamilyTaskManager.Host.Modules.Bot.Helpers;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.UseCases.Pets;
using FamilyTaskManager.UseCases.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TaskStatus = FamilyTaskManager.Core.TaskAggregate.TaskStatus;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.CallbackHandlers;

public class PetCallbackHandler(
  ILogger<PetCallbackHandler> logger,
  IMediator mediator)
  : BaseCallbackHandler(logger, mediator)
{
  public async Task StartCreatePetAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    UserSession session,
    CancellationToken cancellationToken)
  {
    if (session.CurrentFamilyId == null)
    {
      await SendErrorAsync(botClient, chatId, "❌ Сначала выберите активную семью", cancellationToken);
      return;
    }

    var keyboard = new InlineKeyboardMarkup(PetTypeHelper.GetPetTypeSelectionButtons(true));

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      "🐾 Выберите тип питомца:",
      replyMarkup: keyboard,
      cancellationToken: cancellationToken);
  }

  public async Task HandlePetTypeSelectionAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    string petType,
    UserSession session,
    CancellationToken cancellationToken)
  {
    session.SetState(ConversationState.AwaitingPetName, new() { PetType = petType });

    var petTypeEmoji = PetTypeHelper.GetEmojiFromString(petType);

    var keyboard = StateKeyboardHelper.GetKeyboardForState(ConversationState.AwaitingPetName);

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      $"{petTypeEmoji} Введите имя питомца:" +
      StateKeyboardHelper.GetHintForState(ConversationState.AwaitingPetName),
      cancellationToken: cancellationToken);

    // Send keyboard in a separate message
    if (keyboard != null)
      await botClient.SendTextMessageAsync(
        chatId,
        "Используйте кнопки ниже для управления:",
        replyMarkup: keyboard,
        cancellationToken: cancellationToken);
  }

  public async Task HandlePetActionAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    string[] parts,
    UserSession session,
    User fromUser,
    CancellationToken cancellationToken)
  {
    if (parts.Length < 2) return;

    var petAction = parts[1];

    // Handle "back" action separately as it doesn't have a petId
    if (petAction == "back")
    {
      await HandlePetListAsync(botClient, chatId, messageId, session, fromUser, cancellationToken);
      return;
    }

    if (parts.Length < 3) return;

    var petIdStr = parts[2];

    if (!Guid.TryParse(petIdStr, out var petId)) return;

    switch (petAction)
    {
      case "view":
        await HandleViewPetAsync(botClient, chatId, messageId, petId, session, cancellationToken);
        break;

      case "delete":
        await HandleDeletePetAsync(botClient, chatId, messageId, petId, session, cancellationToken);
        break;

      case "confirmdelete":
        await HandleConfirmDeletePetAsync(botClient, chatId, messageId, petId, session, fromUser, cancellationToken);
        break;

      case "canceldelete":
        await botClient.EditMessageTextAsync(
          chatId,
          messageId,
          "❌ Удаление питомца отменено",
          cancellationToken: cancellationToken);
        break;
    }
  }

  private async Task HandleViewPetAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    Guid petId,
    UserSession session,
    CancellationToken cancellationToken)
  {
    if (session.CurrentFamilyId == null)
    {
      await SendErrorAsync(botClient, chatId, BotConstants.Errors.NoFamily, cancellationToken);
      return;
    }

    // Get pet details
    var getPetsQuery = new GetPetsQuery(session.CurrentFamilyId.Value);
    var petsResult = await Mediator.Send(getPetsQuery, cancellationToken);

    if (!petsResult.IsSuccess)
    {
      await SendErrorAsync(botClient, chatId, "❌ Ошибка загрузки питомца", cancellationToken);
      return;
    }

    var pet = petsResult.Value.FirstOrDefault(p => p.Id == petId);
    if (pet == null)
    {
      await SendErrorAsync(botClient, chatId, "❌ Питомец не найден", cancellationToken);
      return;
    }

    // Get active tasks for the pet
    var getTasksQuery = new GetTasksByPetQuery(petId, session.CurrentFamilyId.Value, TaskStatus.Active);
    var tasksResult = await Mediator.Send(getTasksQuery, cancellationToken);

    var (petEmoji, petTypeText) = GetPetTypeInfo(pet.Type);
    var (moodEmoji, moodText) = PetDisplay.GetMoodInfo(pet.MoodScore);

    var messageText = $"{petEmoji} *{pet.Name}*\n\n" +
                      $"💖 Настроение: {moodEmoji} - {moodText}\n\n";

    // Add tasks section
    if (tasksResult.IsSuccess && tasksResult.Value.Any())
    {
      messageText += $"📝 *{pet.Name} хочет чтобы вы ему помогли:*\n";
      foreach (var task in tasksResult.Value)
        messageText += $"• {task.Title} {task.Points.ToStars()} до {task.DueAtLocal:dd.MM.yyyy HH:mm}💖\n";
    }
    else
    {
      messageText += $"📝 *Все задачи выполнены, {pet.Name} доволен!*\n";
      messageText += "Нет активных задач. Создайте задачи из шаблонов!";
    }

    var keyboard = new InlineKeyboardMarkup(new[]
    {
      new[] { InlineKeyboardButton.WithCallbackData("📋 Шаблоны задач", $"tpl_vp_{petId}") },
      new[] { InlineKeyboardButton.WithCallbackData("🗑️ Удалить питомца", $"pet_delete_{petId}") },
      new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад к списку", "pet_back") }
    });

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      messageText,
      ParseMode.Markdown,
      replyMarkup: keyboard,
      cancellationToken: cancellationToken);
  }

  private async Task HandleDeletePetAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    Guid petId,
    UserSession session,
    CancellationToken cancellationToken)
  {
    if (session.CurrentFamilyId == null)
    {
      await SendErrorAsync(botClient, chatId, BotConstants.Errors.NoFamily, cancellationToken);
      return;
    }

    // Get pet details for confirmation message
    var getPetsQuery = new GetPetsQuery(session.CurrentFamilyId.Value);
    var petsResult = await Mediator.Send(getPetsQuery, cancellationToken);

    if (!petsResult.IsSuccess)
    {
      await SendErrorAsync(botClient, chatId, "❌ Ошибка загрузки питомца", cancellationToken);
      return;
    }

    var pet = petsResult.Value.FirstOrDefault(p => p.Id == petId);
    if (pet == null)
    {
      await SendErrorAsync(botClient, chatId, "❌ Питомец не найден", cancellationToken);
      return;
    }

    var (petEmoji, _) = GetPetTypeInfo(pet.Type);

    // Show confirmation dialog
    var keyboard = new InlineKeyboardMarkup(new[]
    {
      new[] { InlineKeyboardButton.WithCallbackData("✅ Да, удалить питомца", $"pet_confirmdelete_{petId}") },
      new[] { InlineKeyboardButton.WithCallbackData("❌ Отмена", "pet_canceldelete") }
    });

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      $"⚠️ *Удаление питомца*\n\n" +
      $"Вы уверены, что хотите удалить питомца {petEmoji} *{pet.Name}*?\n\n" +
      "🚨 *Внимание!* Это действие необратимо и приведет к:\n" +
      "• Удалению всех шаблонов задач питомца\n" +
      "• Удалению всех связанных задач\n" +
      "• Настроение и статистика питомца перестанут обновляться, но история действий семьи сохранится\n\n" +
      BotConstants.Messages.ConfirmDeletion,
      ParseMode.Markdown,
      replyMarkup: keyboard,
      cancellationToken: cancellationToken);
  }

  private async Task HandleConfirmDeletePetAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    Guid petId,
    UserSession session,
    User fromUser,
    CancellationToken cancellationToken)
  {
    // Delete the pet
    var deletePetCommand = new DeletePetCommand(petId, session.UserId);
    var deleteResult = await Mediator.Send(deletePetCommand, cancellationToken);

    if (!deleteResult.IsSuccess)
    {
      await EditMessageWithErrorAsync(
        botClient,
        chatId,
        messageId,
        $"❌ Ошибка удаления питомца: {deleteResult.Errors.FirstOrDefault()}",
        cancellationToken);
      return;
    }

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      "✅ Питомец успешно удалён!\n\n" +
      "Все связанные шаблоны задач и задачи также удалены, история действий семьи при этом сохранена.",
      cancellationToken: cancellationToken);
  }

  private async Task HandlePetListAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    UserSession session,
    User fromUser,
    CancellationToken cancellationToken)
  {
    if (session.CurrentFamilyId == null)
    {
      await EditMessageWithErrorAsync(botClient, chatId, messageId, BotConstants.Errors.NoFamily, cancellationToken);
      return;
    }

    // Get pets
    var getPetsQuery = new GetPetsQuery(session.CurrentFamilyId.Value);
    var petsResult = await Mediator.Send(getPetsQuery, cancellationToken);

    if (!petsResult.IsSuccess)
    {
      await EditMessageWithErrorAsync(botClient, chatId, messageId, "❌ Ошибка загрузки питомцев", cancellationToken);
      return;
    }

    var pets = petsResult.Value;

    if (!pets.Any())
    {
      await botClient.EditMessageTextAsync(
        chatId,
        messageId,
        "🐾 У вас пока нет питомцев.\n\nАдминистратор может создать питомца.",
        replyMarkup: new(new[]
        {
          InlineKeyboardButton.WithCallbackData("➕ Создать питомца", "create_pet")
        }),
        cancellationToken: cancellationToken);
      return;
    }

    var messageText = BuildPetListMessage(pets);
    var keyboard = BuildPetListKeyboard(pets);

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      messageText,
      ParseMode.Markdown,
      replyMarkup: keyboard,
      cancellationToken: cancellationToken);
  }

  private static string BuildPetListMessage(IEnumerable<PetDto> pets)
  {
    var messageText = "🐾 *Ваши питомцы:*\n\n";

    foreach (var pet in pets)
    {
      var (petEmoji, petTypeText) = GetPetTypeInfo(pet.Type);
      var (moodEmoji, moodText) = PetDisplay.GetMoodInfo(pet.MoodScore);

      messageText += $"{petEmoji} *{pet.Name}*\n";
      messageText += $"   Настроение: {moodEmoji} - {moodText}\n";
    }

    return messageText;
  }

  private static InlineKeyboardMarkup BuildPetListKeyboard(IEnumerable<PetDto> pets)
  {
    var buttons = new List<InlineKeyboardButton[]>();

    // Add button for each pet
    foreach (var pet in pets)
    {
      var (petEmoji, _) = GetPetTypeInfo(pet.Type);

      buttons.Add(new[]
      {
        InlineKeyboardButton.WithCallbackData($"{petEmoji} {pet.Name}", $"pet_view_{pet.Id}")
      });
    }

    // Add create pet button
    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("➕ Создать питомца", "create_pet") });

    return new(buttons);
  }

  private static (string emoji, string text) GetPetTypeInfo(PetType petType) =>
    PetTypeHelper.GetInfo(petType);
}
