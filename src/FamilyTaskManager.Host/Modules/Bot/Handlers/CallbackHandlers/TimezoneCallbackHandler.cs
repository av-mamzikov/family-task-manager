using FamilyTaskManager.Core.Interfaces;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.Host.Modules.Bot.Services;
using FamilyTaskManager.UseCases.Families;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.CallbackHandlers;

public class TimezoneCallbackHandler(
  ILogger<TimezoneCallbackHandler> logger,
  IMediator mediator,
  IUserRegistrationService userRegistrationService,
  ITimeZoneService timeZoneService)
  : BaseCallbackHandler(logger, mediator, userRegistrationService)
{
  public async Task HandleTimezoneSelectionAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    string[] parts,
    UserSession session,
    CancellationToken cancellationToken)
  {
    if (parts.Length < 2)
    {
      return;
    }

    var timezoneId = parts[1];

    // Handle show list request
    if (timezoneId == "showlist")
    {
      await ShowTimezoneListAsync(botClient, chatId, messageId, session, cancellationToken);
      return;
    }

    // Handle geolocation detection request
    if (timezoneId == "detect")
    {
      await RequestLocationAsync(botClient, chatId, messageId, session, cancellationToken);
      return;
    }

    // Handle timezone selection
    await CreateFamilyWithTimezoneAsync(botClient, chatId, messageId, timezoneId, session, cancellationToken);
  }

  private async Task ShowTimezoneListAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    UserSession session,
    CancellationToken cancellationToken)
  {
    var timezoneListKeyboard = GetRussianTimeZoneListKeyboard();

    var listFamilyName = session.Data.TryGetValue("familyName", out var listFamilyNameObj) &&
                         listFamilyNameObj is string fn
      ? fn
      : "вашей семьи";

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      $"🌍 Выберите временную зону для семьи \"{listFamilyName}\":",
      replyMarkup: timezoneListKeyboard,
      cancellationToken: cancellationToken);
  }

  private async Task RequestLocationAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    UserSession session,
    CancellationToken cancellationToken)
  {
    session.State = ConversationState.AwaitingFamilyLocation;

    var locationKeyboard = new ReplyKeyboardMarkup(new[]
      {
        new KeyboardButton("📍 Отправить местоположение") { RequestLocation = true }, new KeyboardButton("⬅️ Назад")
      })
      { ResizeKeyboard = true, OneTimeKeyboard = true };

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      "📍 Нажмите кнопку ниже, чтобы поделиться местоположением:",
      cancellationToken: cancellationToken);

    await botClient.SendTextMessageAsync(
      chatId,
      "🌍 Определение временной зоны по геолокации\n\n" +
      BotConstants.Messages.SendLocation +
      BotConstants.Messages.OrBackToManual,
      replyMarkup: locationKeyboard,
      cancellationToken: cancellationToken);
  }

  private async Task CreateFamilyWithTimezoneAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    string timezoneId,
    UserSession session,
    CancellationToken cancellationToken)
  {
    // Get required data from session
    if (!TryGetSessionData<Guid>(session, "userId", out var userId) ||
        !TryGetSessionData<string>(session, "familyName", out var familyName) ||
        familyName == null)
    {
      session.ClearState();
      await EditMessageWithErrorAsync(
        botClient,
        chatId,
        messageId,
        "❌ Ошибка сессии. Попробуйте создать семью заново.",
        cancellationToken);
      return;
    }

    // Validate timezone
    if (!timeZoneService.IsValidTimeZone(timezoneId))
    {
      await EditMessageWithErrorAsync(
        botClient,
        chatId,
        messageId,
        "❌ Неверная временная зона. Попробуйте снова.",
        cancellationToken);
      return;
    }

    // Create family with selected timezone
    var createFamilyCommand = new CreateFamilyCommand(userId, familyName, timezoneId);
    var result = await Mediator.Send(createFamilyCommand, cancellationToken);

    if (!result.IsSuccess)
    {
      await EditMessageWithErrorAsync(
        botClient,
        chatId,
        messageId,
        $"❌ Ошибка создания семьи: {result.Errors.FirstOrDefault()}",
        cancellationToken);
      session.ClearState();
      return;
    }

    session.CurrentFamilyId = result.Value;
    session.ClearState();

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      BotConstants.Success.FamilyCreatedMessage(familyName) +
      $"🌍 Временная зона: {timezoneId}\n\n" +
      BotConstants.Success.NextStepsMessage,
      ParseMode.Markdown,
      cancellationToken: cancellationToken);
  }

  private static InlineKeyboardMarkup GetRussianTimeZoneListKeyboard() =>
    new(new[]
    {
      new[] { InlineKeyboardButton.WithCallbackData("🇷🇺 Калининград", "timezone_Europe/Kaliningrad") },
      new[] { InlineKeyboardButton.WithCallbackData("🇷🇺 Москва", "timezone_Europe/Moscow") },
      new[] { InlineKeyboardButton.WithCallbackData("🇷🇺 Самара", "timezone_Europe/Samara") },
      new[] { InlineKeyboardButton.WithCallbackData("🇷🇺 Екатеринбург", "timezone_Asia/Yekaterinburg") },
      new[] { InlineKeyboardButton.WithCallbackData("🇷🇺 Омск", "timezone_Asia/Omsk") },
      new[] { InlineKeyboardButton.WithCallbackData("🇷🇺 Красноярск", "timezone_Asia/Krasnoyarsk") },
      new[] { InlineKeyboardButton.WithCallbackData("🇷🇺 Иркутск", "timezone_Asia/Irkutsk") },
      new[] { InlineKeyboardButton.WithCallbackData("🇷🇺 Якутск", "timezone_Asia/Yakutsk") },
      new[] { InlineKeyboardButton.WithCallbackData("🇷🇺 Владивосток", "timezone_Asia/Vladivostok") },
      new[] { InlineKeyboardButton.WithCallbackData("🇷🇺 Магадан", "timezone_Asia/Magadan") },
      new[] { InlineKeyboardButton.WithCallbackData("🇷🇺 Камчатка", "timezone_Asia/Kamchatka") },
      new[] { InlineKeyboardButton.WithCallbackData("⏭️ Пропустить (UTC)", "timezone_UTC") }
    });
}
