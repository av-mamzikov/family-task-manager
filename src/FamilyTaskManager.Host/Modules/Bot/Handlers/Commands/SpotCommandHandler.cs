using FamilyTaskManager.Core.SpotAggregate;
using FamilyTaskManager.Host.Modules.Bot.Constants;
using FamilyTaskManager.Host.Modules.Bot.Helpers;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.UseCases.Spots;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.Commands;

public class SpotCommandHandler(IMediator mediator)
{
  public async Task HandleAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    Guid userId,
    CancellationToken cancellationToken)
  {
    if (session.CurrentFamilyId == null)
    {
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        BotMessages.Errors.NoFamily,
        cancellationToken: cancellationToken);
      return;
    }

    // Get spots
    var getSpotsQuery = new GetSpotsQuery(session.CurrentFamilyId.Value);
    var spotsResult = await mediator.Send(getSpotsQuery, cancellationToken);

    if (!spotsResult.IsSuccess)
    {
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        "❌ Ошибка загрузки спотов",
        cancellationToken: cancellationToken);
      return;
    }

    var spots = spotsResult.Value;

    if (!spots.Any())
    {
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        "🐾 У вас пока нет спотов.\n\nАдминистратор может создать спота.",
        replyMarkup: new InlineKeyboardMarkup(new[]
        {
          InlineKeyboardButton.WithCallbackData("➕ Создать спота", CallbackData.Spot.Create)
        }),
        cancellationToken: cancellationToken);
      return;
    }

    var messageText = "🐾 *Ваши споты:*\n\n";

    foreach (var spot in spots)
    {
      var spotEmoji = GetSpotEmoji(spot.Type);
      var moodEmoji = GetMoodEmoji(spot.MoodScore);
      var moodText = GetMoodText(spot.MoodScore);

      messageText += $"{spotEmoji} *{spot.Name}*\n";
      messageText += $"   Настроение: {moodEmoji} - {moodText}\n";
    }

    // Build inline keyboard with Spot actions
    var buttons = new List<InlineKeyboardButton[]>();

    // Add button for each Spot
    foreach (var spot in spots)
    {
      var spotEmoji = GetSpotEmoji(spot.Type);
      buttons.Add(new[]
      {
        InlineKeyboardButton.WithCallbackData($"{spotEmoji} {spot.Name}", CallbackData.Spot.View(spot.Id))
      });
    }

    // Add create Spot button
    buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("➕ Создать спота", CallbackData.Spot.Create) });

    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      messageText,
      parseMode: ParseMode.Markdown,
      replyMarkup: new InlineKeyboardMarkup(buttons),
      cancellationToken: cancellationToken);
  }

  private string GetSpotEmoji(SpotType type) => SpotTypeHelper.GetEmoji(type);

  private string GetSpotTySpotext(SpotType type) => SpotTypeHelper.GetDisplayText(type);

  private string GetMoodEmoji(int moodScore) =>
    moodScore switch
    {
      >= 80 => "😊",
      >= 60 => "🙂",
      >= 40 => "😐",
      >= 20 => "😟",
      _ => "😢"
    };

  private string GetMoodText(int moodScore) =>
    moodScore switch
    {
      >= 80 => "Отлично!",
      >= 60 => "Хорошо",
      >= 40 => "Нормально",
      >= 20 => "Грустит",
      _ => "Очень грустно"
    };
}
