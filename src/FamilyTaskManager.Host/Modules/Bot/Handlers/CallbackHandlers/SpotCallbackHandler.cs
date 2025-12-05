using FamilyTaskManager.Core.SpotAggregate;
using FamilyTaskManager.Host.Modules.Bot.Constants;
using FamilyTaskManager.Host.Modules.Bot.Helpers;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.UseCases.Spots;
using FamilyTaskManager.UseCases.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TaskStatus = FamilyTaskManager.Core.TaskAggregate.TaskStatus;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.CallbackHandlers;

public class SpotCallbackHandler(
  ILogger<SpotCallbackHandler> logger,
  IMediator mediator)
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
    await HandleSpotActionAsync(botClient, chatId, messageId, parts, session, fromUser, cancellationToken);

  public async Task StartCreateSpotAsync(
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

    var keyboard = new InlineKeyboardMarkup(SpotTypeHelper.GetSpotTypeSelectionButtons(true));

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      "🐾 Выберите тип спота:",
      replyMarkup: keyboard,
      cancellationToken: cancellationToken);
  }

  public async Task HandleSpotTypeSelectionAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    string spotType,
    UserSession session,
    CancellationToken cancellationToken)
  {
    session.State = ConversationState.SpotCreation;
    session.Data = new() { SpotType = spotType, InternalState = "awaiting_name" };

    var spotTypeEmoji = SpotTypeHelper.GetEmojiFromString(spotType);

    var keyboard = new ReplyKeyboardMarkup(new[] { new KeyboardButton[] { new("❌ Отменить") } })
    {
      ResizeKeyboard = true
    };

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      $"{spotTypeEmoji} Введите имя спота {spotTypeEmoji}:\n\n💡 Используйте кнопку \"❌ Отменить\" для отмены.",
      cancellationToken: cancellationToken);

    // Send keyboard in a separate message
    await botClient.SendTextMessageAsync(
      chatId,
      "Используйте кнопки ниже для управления:",
      replyMarkup: keyboard,
      cancellationToken: cancellationToken);
  }

  private async Task HandleSpotActionAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    string[] parts,
    UserSession session,
    User fromUser,
    CancellationToken cancellationToken)
  {
    if (parts.Length < 2) return;

    var spotAction = parts[1];

    // Handle select action for spot type selection
    if (spotAction == CallbackActions.Select && parts.Length >= 3)
    {
      await HandleSpotTypeSelectionAsync(botClient, chatId, messageId, parts[2], session, cancellationToken);
      return;
    }

    // Handle actions that don't require a spotId
    if (spotAction == CallbackActions.Back)
    {
      await HandleSpotListAsync(botClient, chatId, messageId, session, fromUser, cancellationToken);
      return;
    }

    if (spotAction == CallbackActions.Create)
    {
      await StartCreateSpotAsync(botClient, chatId, messageId, session, cancellationToken);
      return;
    }

    if (parts.Length < 3) return;

    var spotIdStr = parts[2];

    if (!Guid.TryParse(spotIdStr, out var spotId)) return;

    switch (spotAction)
    {
      case var _ when spotAction == CallbackActions.View:
        await HandleViewSpotAsync(botClient, chatId, messageId, spotId, session, cancellationToken);
        break;

      case var _ when spotAction == CallbackActions.Delete:
        await HandleDeleteSpotAsync(botClient, chatId, messageId, spotId, session, cancellationToken);
        break;

      case var _ when spotAction == CallbackActions.ConfirmDelete:
        await HandleConfirmDeleteSpotAsync(botClient, chatId, messageId, spotId, session, fromUser, cancellationToken);
        break;

      case var _ when spotAction == CallbackActions.CancelDelete:
        await botClient.EditMessageTextAsync(
          chatId,
          messageId,
          "❌ Удаление спота отменено",
          cancellationToken: cancellationToken);
        break;
    }
  }

  private async Task HandleViewSpotAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    Guid spotId,
    UserSession session,
    CancellationToken cancellationToken)
  {
    if (session.CurrentFamilyId == null)
    {
      await SendErrorAsync(botClient, chatId, BotMessages.Errors.NoFamily, cancellationToken);
      return;
    }

    // Get spot details
    var getSpotQuery = new GetSpotsQuery(session.CurrentFamilyId.Value);
    var spotsResult = await Mediator.Send(getSpotQuery, cancellationToken);

    if (!spotsResult.IsSuccess)
    {
      await SendErrorAsync(botClient, chatId, "❌ Ошибка загрузки спота", cancellationToken);
      return;
    }

    var spot = spotsResult.Value.FirstOrDefault(p => p.Id == spotId);
    if (spot == null)
    {
      await SendErrorAsync(botClient, chatId, "❌ Спот не найден", cancellationToken);
      return;
    }

    // Get active tasks for the spot
    var getTasksQuery = new GetTasksBySpotQuery(spotId, session.CurrentFamilyId.Value, TaskStatus.Active);
    var tasksResult = await Mediator.Send(getTasksQuery, cancellationToken);

    var (spotEmoji, spotTySpotext) = GetSoptTypeInfo(spot.Type);
    var (moodEmoji, moodText) = SpotDisplay.GetMoodInfo(spot.MoodScore);

    var messageText = $"{spotEmoji} *{spot.Name}*\n\n" +
                      $"💖 Настроение: {moodEmoji} - {moodText}\n\n";

    // Add tasks section
    if (tasksResult.IsSuccess && tasksResult.Value.Any())
    {
      messageText += $"📝 *{spot.Name} хочет чтобы вы ему помогли:*\n";
      foreach (var task in tasksResult.Value)
        messageText += $"• {task.Title} {task.Points.ToStars()} до {task.DueAtLocal:dd.MM.yyyy HH:mm}💖\n";
    }
    else
    {
      messageText += $"📝 *Все задачи выполнены, {spot.Name} доволен!*\n";
      messageText += "Нет активных задач. Создайте задачи из шаблонов!";
    }

    var keyboard = new InlineKeyboardMarkup(new[]
    {
      new[] { InlineKeyboardButton.WithCallbackData("📋 Шаблоны задач", CallbackData.Templates.ViewForSpot(spotId)) },
      new[] { InlineKeyboardButton.WithCallbackData("🗑️ Удалить спота", CallbackData.Spot.Delete(spotId)) },
      new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад к списку", CallbackData.Spot.Back) }
    });

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      messageText,
      ParseMode.Markdown,
      replyMarkup: keyboard,
      cancellationToken: cancellationToken);
  }

  private async Task HandleDeleteSpotAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    Guid spotId,
    UserSession session,
    CancellationToken cancellationToken)
  {
    if (session.CurrentFamilyId == null)
    {
      await SendErrorAsync(botClient, chatId, BotMessages.Errors.NoFamily, cancellationToken);
      return;
    }

    // Get spot details for confirmation message
    var getSpotsQuery = new GetSpotsQuery(session.CurrentFamilyId.Value);
    var spotsResult = await Mediator.Send(getSpotsQuery, cancellationToken);

    if (!spotsResult.IsSuccess)
    {
      await SendErrorAsync(botClient, chatId, "❌ Ошибка загрузки спота", cancellationToken);
      return;
    }

    var spot = spotsResult.Value.FirstOrDefault(p => p.Id == spotId);
    if (spot == null)
    {
      await SendErrorAsync(botClient, chatId, "❌ Спот не найден", cancellationToken);
      return;
    }

    var (spotEmoji, _) = GetSoptTypeInfo(spot.Type);

    // Show confirmation dialog
    var keyboard = new InlineKeyboardMarkup(new[]
    {
      new[] { InlineKeyboardButton.WithCallbackData("✅ Да, удалить спота", CallbackData.Spot.ConfirmDelete(spotId)) },
      new[] { InlineKeyboardButton.WithCallbackData("❌ Отмена", CallbackData.Spot.CancelDelete) }
    });

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      $"⚠️ *Удаление спота*\n\n" +
      $"Вы уверены, что хотите удалить спота {spotEmoji} *{spot.Name}*?\n\n" +
      "🚨 *Внимание!* Это действие необратимо и приведет к:\n" +
      "• Удалению всех шаблонов задач спота\n" +
      "• Удалению всех связанных задач\n" +
      "• Настроение и статистика спота перестанут обновляться, но история действий семьи сохранится\n\n" +
      BotMessages.Messages.ConfirmDeletion,
      ParseMode.Markdown,
      replyMarkup: keyboard,
      cancellationToken: cancellationToken);
  }

  private async Task HandleConfirmDeleteSpotAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    Guid spotId,
    UserSession session,
    User fromUser,
    CancellationToken cancellationToken)
  {
    // Delete the Spot
    var deleteSpotCommand = new DeleteSpotCommand(spotId, session.UserId);
    var deleteResult = await Mediator.Send(deleteSpotCommand, cancellationToken);

    if (!deleteResult.IsSuccess)
    {
      await EditMessageWithErrorAsync(
        botClient,
        chatId,
        messageId,
        $"❌ Ошибка удаления спота: {deleteResult.Errors.FirstOrDefault()}",
        cancellationToken);
      return;
    }

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      "✅ Спот успешно удалён!\n\n" +
      "Все связанные шаблоны задач и задачи также удалены, история действий семьи при этом сохранена.",
      cancellationToken: cancellationToken);
  }

  private async Task HandleSpotListAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    UserSession session,
    User fromUser,
    CancellationToken cancellationToken)
  {
    if (session.CurrentFamilyId == null)
    {
      await EditMessageWithErrorAsync(botClient, chatId, messageId, BotMessages.Errors.NoFamily, cancellationToken);
      return;
    }

    // Get Spots
    var getSpotsQuery = new GetSpotsQuery(session.CurrentFamilyId.Value);
    var SpotsResult = await Mediator.Send(getSpotsQuery, cancellationToken);

    if (!SpotsResult.IsSuccess)
    {
      await EditMessageWithErrorAsync(botClient, chatId, messageId, "❌ Ошибка загрузки спотов", cancellationToken);
      return;
    }

    var Spots = SpotsResult.Value;

    if (!Spots.Any())
    {
      await botClient.EditMessageTextAsync(
        chatId,
        messageId,
        "🐾 У вас пока нет спотов.\n\nАдминистратор может создать спота.",
        replyMarkup: new(new[]
        {
          InlineKeyboardButton.WithCallbackData("➕ Создать спота", CallbackData.Spot.Create)
        }),
        cancellationToken: cancellationToken);
      return;
    }

    var messageText = BuildSpotListMessage(Spots);
    var keyboard = BuildSpotListKeyboard(Spots);

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      messageText,
      ParseMode.Markdown,
      replyMarkup: keyboard,
      cancellationToken: cancellationToken);
  }

  private static string BuildSpotListMessage(IEnumerable<SpotDto> spots)
  {
    var messageText = "🐾 *Ваши споты:*\n\n";

    foreach (var spot in spots)
    {
      var (spotEmoji, spotTySpotext) = GetSoptTypeInfo(spot.Type);
      var (moodEmoji, moodText) = SpotDisplay.GetMoodInfo(spot.MoodScore);

      messageText += $"{spotEmoji} *{spot.Name}*\n";
      messageText += $"   Настроение: {moodEmoji} - {moodText}\n";
    }

    return messageText;
  }

  private static InlineKeyboardMarkup BuildSpotListKeyboard(IEnumerable<SpotDto> spots)
  {
    var buttons = new List<InlineKeyboardButton[]>();

    // Add button for each Spot
    foreach (var spot in spots)
    {
      var (spotEmoji, _) = GetSoptTypeInfo(spot.Type);

      buttons.Add(new[]
      {
        InlineKeyboardButton.WithCallbackData($"{spotEmoji} {spot.Name}", CallbackData.Spot.View(spot.Id))
      });
    }

    // Add create Spot button
    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("➕ Создать спота", CallbackData.Spot.Create) });

    return new(buttons);
  }

  private static (string emoji, string text) GetSoptTypeInfo(SpotType spotType) =>
    SpotTypeHelper.GetInfo(spotType);
}
