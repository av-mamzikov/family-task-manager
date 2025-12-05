using FamilyTaskManager.Core.Interfaces;
using FamilyTaskManager.Host.Modules.Bot.Constants;
using FamilyTaskManager.Host.Modules.Bot.Helpers;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.UseCases.Families;
using GeoTimeZone;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.ConversationHandlers;

public class FamilyCreationHandler(
  ILogger<FamilyCreationHandler> logger,
  IMediator mediator,
  ITimeZoneService timeZoneService)
  : BaseConversationHandler(logger, mediator), IConversationHandler
{
  private const string StateAwaitingName = "awaiting_name";
  private const string StateAwaitingTimezone = "awaiting_timezone";
  private const string StateAwaitingLocation = "awaiting_location";

  public async Task HandleMessageAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    CancellationToken cancellationToken)
  {
    if (message.Location != null && session.Data.InternalState == StateAwaitingLocation)
    {
      await HandleFamilyLocationInputAsync(botClient, message, session, cancellationToken);
      return;
    }

    var text = message.Text;
    if (string.IsNullOrWhiteSpace(text))
      return;

    if (text is "❌ Отменить" or "/cancel" or "⬅️ Назад")
      return;

    await (session.Data.InternalState switch
    {
      StateAwaitingName => HandleFamilyNameInputAsync(botClient, message, session, text, cancellationToken),
      StateAwaitingTimezone => HandleTimezoneTextInput(botClient, message, cancellationToken),
      StateAwaitingLocation => HandleLocationTextInput(botClient, message, session, cancellationToken),
      _ => HandleUnknownState(botClient, message, session, cancellationToken)
    });
  }

  public async Task HandleCallbackAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    string[] callbackParts,
    UserSession session,
    User fromUser,
    CancellationToken cancellationToken)
  {
    if (callbackParts.Length < 2 || callbackParts[0] != CallbackData.Timezone.Entity)
      return;

    var timezoneId = callbackParts[1];

    if (timezoneId == CallbackActions.ShowList)
    {
      await ShowTimezoneListAsync(botClient, chatId, messageId, session, cancellationToken);
      return;
    }

    if (timezoneId == CallbackActions.Detect)
    {
      await RequestLocationAsync(botClient, chatId, messageId, session, cancellationToken);
      return;
    }

    await CreateFamilyWithTimezoneAsync(botClient, chatId, messageId, timezoneId, session, cancellationToken);
  }

  private async Task HandleFamilyNameInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string familyName,
    CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(familyName) || familyName.Length < 3)
    {
      var keyboard = GetCancelKeyboard();
      await SendValidationErrorAsync(
        botClient,
        message.Chat.Id,
        BotMessages.Errors.FamilyNameTooShort,
        "\n\n💡 Используйте кнопку \"❌ Отменить\" для отмены.",
        keyboard,
        cancellationToken);
      return;
    }

    session.Data.FamilyName = familyName;
    session.Data.InternalState = StateAwaitingTimezone;

    var timezoneKeyboard = GetTimezoneChoiceKeyboard();

    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      BotMessages.Messages.ChooseTimezoneMethod(familyName),
      replyMarkup: timezoneKeyboard,
      cancellationToken: cancellationToken);
  }

  private async Task HandleFamilyLocationInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    CancellationToken cancellationToken)
  {
    var location = message.Location;

    // Defensive null check
    if (location?.Latitude == null || location?.Longitude == null)
    {
      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        BotMessages.Errors.InvalidLocationData +
        BotMessages.Errors.TryAgain,
        replyMarkup: new ReplyKeyboardRemove(),
        cancellationToken: cancellationToken);

      await HandleBackToTimezoneSelectionAsync(botClient, message, session, cancellationToken);
      return;
    }

    try
    {
      // Convert coordinates to timezone using GeoTimeZone
      var timeZoneResult = TimeZoneLookup.GetTimeZone(location.Latitude, location.Longitude);
      var detectedTimezone = timeZoneResult.Result;

      // Add null check for ocean/invalid coordinates
      if (string.IsNullOrEmpty(detectedTimezone))
      {
        await botClient.SendTextMessageAsync(
          message.Chat.Id,
          BotMessages.Errors.TimezoneDetectionFailed,
          replyMarkup: new ReplyKeyboardRemove(),
          cancellationToken: cancellationToken);

        await HandleBackToTimezoneSelectionAsync(botClient, message, session, cancellationToken);
        return;
      }

      Logger.LogInformation("Detected timezone for coordinates {Lat}, {Lng}: {Timezone}",
        location.Latitude, location.Longitude, detectedTimezone);

      // Get required data from session
      if (session.Data.FamilyName == null)
      {
        await SendErrorAndClearStateAsync(
          botClient,
          message.Chat.Id,
          session,
          BotMessages.Errors.SessionErrorRetry,
          cancellationToken);
        return;
      }

      // Validate detected timezone
      if (!timeZoneService.IsValidTimeZone(detectedTimezone))
      {
        await botClient.SendTextMessageAsync(
          message.Chat.Id,
          BotMessages.Errors.TimezoneValidationFailed +
          BotMessages.Errors.ChooseTimezoneManually,
          replyMarkup: new ReplyKeyboardRemove(),
          cancellationToken: cancellationToken);

        await HandleBackToTimezoneSelectionAsync(botClient, message, session, cancellationToken);
        return;
      }

      // Create family with detected timezone
      var createFamilyCommand = new CreateFamilyCommand(session.UserId, session.Data.FamilyName, detectedTimezone);
      var result = await Mediator.Send(createFamilyCommand, cancellationToken);

      if (!result.IsSuccess)
      {
        await SendErrorAndClearStateAsync(
          botClient,
          message.Chat.Id,
          session,
          BotMessages.Errors.FamilyCreationError(result.Errors.FirstOrDefault()),
          cancellationToken);
        return;
      }

      session.CurrentFamilyId = result.Value;

      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        BotMessages.Messages.FamilyCreatedWithTimezone(session.Data.FamilyName, detectedTimezone),
        parseMode: ParseMode.Markdown,
        replyMarkup: new ReplyKeyboardRemove(),
        cancellationToken: cancellationToken);

      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        "🏠 Главное меню",
        replyMarkup: MainMenuHelper.GetMainMenuKeyboard(),
        cancellationToken: cancellationToken);

      session.ClearState();
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "Error determining timezone from location");

      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        BotMessages.Errors.LocationError +
        BotMessages.Errors.TryAgainOrChooseTimezone,
        replyMarkup: new ReplyKeyboardRemove(),
        cancellationToken: cancellationToken);

      await HandleBackToTimezoneSelectionAsync(botClient, message, session, cancellationToken);
    }
  }

  private async Task HandleBackToTimezoneSelectionAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    CancellationToken cancellationToken)
  {
    session.Data.InternalState = StateAwaitingTimezone;

    var keyboard = GetTimezoneChoiceKeyboard();

    var familyName = session.Data.FamilyName ?? "ваша семья";

    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      BotMessages.Messages.ChooseTimezoneMethod(familyName),
      replyMarkup: keyboard,
      cancellationToken: cancellationToken);
  }

  private static InlineKeyboardMarkup GetTimezoneChoiceKeyboard() =>
    new([
      [InlineKeyboardButton.WithCallbackData("📍 Определить по геолокации", CallbackData.Timezone.Detect)],
      [InlineKeyboardButton.WithCallbackData("📋 Выбрать из списка", CallbackData.Timezone.ShowList)]
    ]);

  private static ReplyKeyboardMarkup GetCancelKeyboard() =>
    new([[new("❌ Отменить")]])
    {
      ResizeKeyboard = true
    };

  private static async Task HandleTimezoneTextInput(
    ITelegramBotClient botClient,
    Message message,
    CancellationToken cancellationToken) =>
    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      "❌ Пожалуйста, используйте кнопки для выбора временной зоны.",
      cancellationToken: cancellationToken);

  private async Task HandleLocationTextInput(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    CancellationToken cancellationToken)
  {
    if (message.Text == "⬅️ Назад")
    {
      await HandleBackToTimezoneSelectionAsync(botClient, message, session, cancellationToken);
      return;
    }

    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      "❌ Пожалуйста, используйте кнопку \"📍 Отправить местоположение\" для определения временной зоны.",
      cancellationToken: cancellationToken);
  }

  private static async Task HandleUnknownState(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    CancellationToken cancellationToken)
  {
    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      "❌ Произошла ошибка. Попробуйте снова.",
      cancellationToken: cancellationToken);
    session.ClearState();
  }

  private async Task ShowTimezoneListAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    UserSession session,
    CancellationToken cancellationToken)
  {
    var timezoneListKeyboard = GetRussianTimeZoneListKeyboard();
    var listFamilyName = session.Data.FamilyName ?? "вашей семьи";

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
    session.Data.InternalState = StateAwaitingLocation;

    var locationKeyboard = new ReplyKeyboardMarkup([
      [new("📍 Отправить местоположение") { RequestLocation = true }],
      [new("⬅️ Назад")]
    ])
    {
      ResizeKeyboard = true
    };

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      "📍 Нажмите кнопку ниже, чтобы поделиться местоположением:",
      cancellationToken: cancellationToken);

    await botClient.SendTextMessageAsync(
      chatId,
      "🌍 Определение временной зоны по геолокации\n\n" +
      BotMessages.Messages.SendLocation +
      BotMessages.Messages.OrBackToManual,
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
    if (session.Data.FamilyName == null)
    {
      session.ClearState();
      await botClient.EditMessageTextAsync(
        chatId,
        messageId,
        "❌ Ошибка сессии. Попробуйте создать семью заново.",
        cancellationToken: cancellationToken);
      return;
    }

    if (!timeZoneService.IsValidTimeZone(timezoneId))
    {
      await botClient.EditMessageTextAsync(
        chatId,
        messageId,
        "❌ Неверная временная зона. Попробуйте снова.",
        cancellationToken: cancellationToken);
      return;
    }

    var createFamilyCommand = new CreateFamilyCommand(session.UserId, session.Data.FamilyName, timezoneId);
    var result = await Mediator.Send(createFamilyCommand, cancellationToken);

    if (!result.IsSuccess)
    {
      await botClient.EditMessageTextAsync(
        chatId,
        messageId,
        $"❌ Ошибка создания семьи: {result.Errors.FirstOrDefault()}",
        cancellationToken: cancellationToken);
      session.ClearState();
      return;
    }

    session.CurrentFamilyId = result.Value;

    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      BotMessages.Success.FamilyCreatedMessage(session.Data.FamilyName) +
      $"🌍 Временная зона: {timezoneId}\n\n" +
      BotMessages.Success.NextStepsMessage,
      ParseMode.Markdown,
      cancellationToken: cancellationToken);

    await botClient.SendTextMessageAsync(
      chatId,
      "🏠 Главное меню",
      replyMarkup: MainMenuHelper.GetMainMenuKeyboard(),
      cancellationToken: cancellationToken);
    session.ClearState();
  }

  private static InlineKeyboardMarkup GetRussianTimeZoneListKeyboard() =>
    new([
      [InlineKeyboardButton.WithCallbackData("🇷🇺 Калининград", CallbackData.Timezone.EuropeKaliningrad)],
      [InlineKeyboardButton.WithCallbackData("🇷🇺 Москва", CallbackData.Timezone.EuropeMoscow)],
      [InlineKeyboardButton.WithCallbackData("🇷🇺 Самара", CallbackData.Timezone.EuropeSamara)],
      [InlineKeyboardButton.WithCallbackData("🇷🇺 Екатеринбург", CallbackData.Timezone.AsiaYekaterinburg)],
      [InlineKeyboardButton.WithCallbackData("🇷🇺 Омск", CallbackData.Timezone.AsiaOmsk)],
      [InlineKeyboardButton.WithCallbackData("🇷🇺 Красноярск", CallbackData.Timezone.AsiaKrasnoyarsk)],
      [InlineKeyboardButton.WithCallbackData("🇷🇺 Иркутск", CallbackData.Timezone.AsiaIrkutsk)],
      [InlineKeyboardButton.WithCallbackData("🇷🇺 Якутск", CallbackData.Timezone.AsiaYakutsk)],
      [InlineKeyboardButton.WithCallbackData("🇷🇺 Владивосток", CallbackData.Timezone.AsiaVladivostok)],
      [InlineKeyboardButton.WithCallbackData("🇷🇺 Магадан", CallbackData.Timezone.AsiaMagadan)],
      [InlineKeyboardButton.WithCallbackData("🇷🇺 Камчатка", CallbackData.Timezone.AsiaKamchatka)],
      [InlineKeyboardButton.WithCallbackData("⏭️ Пропустить (UTC)", CallbackData.Timezone.Utc)]
    ]);
}
