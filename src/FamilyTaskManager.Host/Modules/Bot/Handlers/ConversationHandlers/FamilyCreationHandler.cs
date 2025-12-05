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
  : BaseConversationHandler(logger, mediator)
{
  public async Task HandleFamilyNameInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string familyName,
    CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(familyName) || familyName.Length < 3)
    {
      var keyboard = StateKeyboardHelper.GetKeyboardForState(ConversationState.AwaitingFamilyName);
      await SendValidationErrorAsync(
        botClient,
        message.Chat.Id,
        BotMessages.Errors.FamilyNameTooShort,
        StateKeyboardHelper.GetHintForState(ConversationState.AwaitingFamilyName),
        keyboard,
        cancellationToken);
      return;
    }

    // Store family name and ask for timezone
    session.Data.FamilyName = familyName;
    session.State = ConversationState.AwaitingFamilyTimezone;

    var timezoneKeyboard = GetTimezoneChoiceKeyboard();

    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      BotMessages.Messages.ChooseTimezoneMethod(familyName),
      replyMarkup: timezoneKeyboard,
      cancellationToken: cancellationToken);
  }

  public async Task HandleFamilyLocationInputAsync(
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

  public async Task HandleBackToTimezoneSelectionAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    CancellationToken cancellationToken)
  {
    session.State = ConversationState.AwaitingFamilyTimezone;

    var keyboard = GetTimezoneChoiceKeyboard();

    var familyName = session.Data.FamilyName ?? "ваша семья";

    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      BotMessages.Messages.ChooseTimezoneMethod(familyName),
      replyMarkup: keyboard,
      cancellationToken: cancellationToken);
  }

  private static InlineKeyboardMarkup GetTimezoneChoiceKeyboard() =>
    new(new[]
    {
      new[] { InlineKeyboardButton.WithCallbackData("📍 Определить по геолокации", "timezone_detect") },
      new[] { InlineKeyboardButton.WithCallbackData("📋 Выбрать из списка", "timezone_showlist") }
    });
}
