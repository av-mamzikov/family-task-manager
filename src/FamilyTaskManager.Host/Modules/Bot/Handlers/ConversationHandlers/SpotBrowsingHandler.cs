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

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.ConversationHandlers;

public class SpotBrowsingHandler(
  ILogger<SpotBrowsingHandler> logger,
  IMediator mediator)
  : BaseConversationHandler(logger), IConversationHandler
{
  public Task HandleMessageAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    CancellationToken cancellationToken) => Task.CompletedTask;

  public async Task HandleCallbackAsync(ITelegramBotClient botClient,
    long chatId,
    Message? message,
    string[] callbackParts,
    UserSession session,
    User fromUser,
    CancellationToken cancellationToken)
  {
    if (callbackParts.Length < 2) return;

    if (callbackParts.IsCallbackOf((Func<EncodedGuid, string>)CallbackData.SpotBrowsing.View,
          out var viewSpotId))
      await HandleViewSpotAsync(botClient, chatId, message, viewSpotId, session, cancellationToken);
    else if (callbackParts.IsCallbackOf((Func<EncodedGuid, string>)CallbackData.SpotBrowsing.Delete,
               out var deleteSpotId))
      await HandleDeleteSpotAsync(botClient, chatId, message, deleteSpotId, session, cancellationToken);
    else if (callbackParts.IsCallbackOf((Func<EncodedGuid, string>)CallbackData.SpotBrowsing.ConfirmDelete,
               out var confirmDeleteSpotId))
      await HandleConfirmDeleteSpotAsync(botClient, chatId, message, confirmDeleteSpotId, session, cancellationToken);
    else if (callbackParts.IsCallbackOf(CallbackData.SpotBrowsing.List))
      await ShowSpotListAsync(botClient, chatId, message, session, cancellationToken);
  }

  public async Task HandleBackAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    Func<Task> sendMainMenuAction,
    CancellationToken cancellationToken)
  {
    await sendMainMenuAction();
    session.ClearState();
  }

  public async Task ShowSpotListAsync(
    ITelegramBotClient botClient,
    long chatId,
    Message? message,
    UserSession session,
    CancellationToken cancellationToken)
  {
    if (session.CurrentFamilyId == null)
    {
      await EditMessageWithErrorAsync(botClient, chatId, message, BotMessages.Errors.NoFamily, cancellationToken);
      return;
    }

    var getSpotsQuery = new GetSpotsQuery(session.CurrentFamilyId.Value);
    var spotsResult = await mediator.Send(getSpotsQuery, cancellationToken);

    if (!spotsResult.IsSuccess)
    {
      await EditMessageWithErrorAsync(botClient, chatId, message, "❌ Ошибка загрузки спотов", cancellationToken);
      return;
    }

    var spots = spotsResult.Value;

    if (!spots.Any())
    {
      await botClient.SendOrEditMessageAsync(
        chatId,
        message,
        "🧩 У вас пока нет спотов.\n\nАдминистратор может создать спота.",
        replyMarkup: new InlineKeyboardMarkup([
          InlineKeyboardButton.WithCallbackData("➕ Создать спота", CallbackData.SpotCreation.Start())
        ]),
        cancellationToken: cancellationToken);
      return;
    }

    var messageText = BuildSpotListMessage(spots);
    var keyboard = BuildSpotListKeyboard(spots);

    await botClient.SendOrEditMessageAsync(
      chatId,
      message,
      messageText,
      ParseMode.Markdown,
      keyboard,
      cancellationToken);
  }

  private async Task HandleViewSpotAsync(
    ITelegramBotClient botClient,
    long chatId,
    Message? message,
    Guid spotId,
    UserSession session,
    CancellationToken cancellationToken)
  {
    if (session.CurrentFamilyId == null)
    {
      await SendErrorAsync(botClient, chatId, BotMessages.Errors.NoFamily, cancellationToken);
      return;
    }

    var getSpotQuery = new GetSpotsQuery(session.CurrentFamilyId.Value);
    var spotsResult = await mediator.Send(getSpotQuery, cancellationToken);

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

    var getTasksQuery = new GetTasksBySpotQuery(spotId, session.CurrentFamilyId.Value, TaskStatus.Active);
    var tasksResult = await mediator.Send(getTasksQuery, cancellationToken);

    var (spotEmoji, _) = GetSpotTypeInfo(spot.Type);
    var (moodEmoji, moodText) = SpotDisplay.GetMoodInfo(spot.MoodScore);

    var messageText = $"{spotEmoji} *{spot.Name}*\n\n" +
                      $"💖 Настроение: {moodEmoji} - {moodText}\n\n";

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

    var keyboard = new InlineKeyboardMarkup([
      [InlineKeyboardButton.WithCallbackData("📋 Шаблоны задач", CallbackData.TemplateBrowsing.ListOfSpot(spotId))],
      [InlineKeyboardButton.WithCallbackData("🗑️ Удалить спота", CallbackData.SpotBrowsing.Delete(spotId))],
      [InlineKeyboardButton.WithCallbackData("⬅️ Назад к списку", CallbackData.SpotBrowsing.List())]
    ]);

    await botClient.SendOrEditMessageAsync(
      chatId,
      message,
      messageText,
      ParseMode.Markdown,
      keyboard,
      cancellationToken);
  }

  private async Task HandleDeleteSpotAsync(
    ITelegramBotClient botClient,
    long chatId,
    Message? message,
    Guid spotId,
    UserSession session,
    CancellationToken cancellationToken)
  {
    if (session.CurrentFamilyId == null)
    {
      await SendErrorAsync(botClient, chatId, BotMessages.Errors.NoFamily, cancellationToken);
      return;
    }

    var getSpotsQuery = new GetSpotsQuery(session.CurrentFamilyId.Value);
    var spotsResult = await mediator.Send(getSpotsQuery, cancellationToken);

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

    var (spotEmoji, _) = GetSpotTypeInfo(spot.Type);

    var keyboard = new InlineKeyboardMarkup([
      [InlineKeyboardButton.WithCallbackData("✅ Да, удалить спота", CallbackData.SpotBrowsing.ConfirmDelete(spotId))],
      [InlineKeyboardButton.WithCallbackData("❌ Отмена", CallbackData.SpotBrowsing.List())]
    ]);

    await botClient.SendOrEditMessageAsync(
      chatId,
      message,
      $"⚠️ *Удаление спота*\n\n" +
      $"Вы уверены, что хотите удалить спота {spotEmoji} *{spot.Name}*?\n\n" +
      "🚨 *Внимание!* Это действие необратимо и приведет к:\n" +
      "• Удалению всех шаблонов задач спота\n" +
      "• Удалению всех связанных задач\n" +
      "• Настроение и статистика спота перестанут обновляться, но история действий семьи сохранится\n\n" +
      BotMessages.Messages.ConfirmDeletion,
      ParseMode.Markdown,
      keyboard,
      cancellationToken);
  }

  private async Task HandleConfirmDeleteSpotAsync(
    ITelegramBotClient botClient,
    long chatId,
    Message? message,
    Guid spotId,
    UserSession session,
    CancellationToken cancellationToken)
  {
    var deleteSpotCommand = new DeleteSpotCommand(spotId, session.UserId);
    var deleteResult = await mediator.Send(deleteSpotCommand, cancellationToken);

    if (!deleteResult.IsSuccess)
    {
      await EditMessageWithErrorAsync(
        botClient,
        chatId,
        message,
        $"❌ Ошибка удаления спота: {deleteResult.Errors.FirstOrDefault()}",
        cancellationToken);
      return;
    }

    await botClient.SendOrEditMessageAsync(
      chatId,
      message,
      "✅ Спот успешно удалён!\n\n" +
      "Все связанные шаблоны задач и задачи также удалены, история действий семьи при этом сохранена.",
      cancellationToken: cancellationToken);
  }

  private static string BuildSpotListMessage(IEnumerable<SpotDto> spots)
  {
    var messageText = "🧩 *Ваши споты:*\n\n";

    foreach (var spot in spots)
    {
      var (spotEmoji, _) = GetSpotTypeInfo(spot.Type);
      var (moodEmoji, moodText) = SpotDisplay.GetMoodInfo(spot.MoodScore);

      messageText += $"{spotEmoji} *{spot.Name}*\n";
      messageText += $"   Настроение: {moodEmoji} - {moodText}\n";
    }

    return messageText;
  }

  private static InlineKeyboardMarkup BuildSpotListKeyboard(IEnumerable<SpotDto> spots)
  {
    var buttons = new List<InlineKeyboardButton[]>();

    foreach (var spot in spots)
    {
      var (spotEmoji, _) = GetSpotTypeInfo(spot.Type);
      buttons.Add([
        InlineKeyboardButton.WithCallbackData($"{spotEmoji} {spot.Name}", CallbackData.SpotBrowsing.View(spot.Id))
      ]);
    }

    buttons.Add([InlineKeyboardButton.WithCallbackData("➕ Создать спота", CallbackData.SpotCreation.Start())]);

    return new(buttons);
  }

  private static (string emoji, string text) GetSpotTypeInfo(SpotType spotType) =>
    SpotDisplay.GetInfo(spotType);
}
