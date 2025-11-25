using FamilyTaskManager.Core.Interfaces;
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
        BotConstants.Errors.FamilyNameTooShort,
        StateKeyboardHelper.GetHintForState(ConversationState.AwaitingFamilyName),
        keyboard,
        cancellationToken);
      return;
    }

    // Get userId from session data
    if (!TryGetSessionData<Guid>(session, "userId", out var userId))
    {
      await SendErrorAndClearStateAsync(
        botClient,
        message.Chat.Id,
        session,
        BotConstants.Errors.SessionError,
        cancellationToken);
      return;
    }

    // Store family name and ask for timezone
    session.Data["familyName"] = familyName;
    session.State = ConversationState.AwaitingFamilyTimezone;

    var timezoneKeyboard = GetTimezoneChoiceKeyboard();

    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      BotConstants.Messages.ChooseTimezoneMethod(familyName),
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
        BotConstants.Errors.InvalidLocationData +
        BotConstants.Errors.TryAgain,
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
          BotConstants.Errors.TimezoneDetectionFailed,
          replyMarkup: new ReplyKeyboardRemove(),
          cancellationToken: cancellationToken);

        await HandleBackToTimezoneSelectionAsync(botClient, message, session, cancellationToken);
        return;
      }

      Logger.LogInformation("Detected timezone for coordinates {Lat}, {Lng}: {Timezone}",
        location.Latitude, location.Longitude, detectedTimezone);

      // Get required data from session
      if (!TryGetSessionData<Guid>(session, "userId", out var userId) ||
          !TryGetSessionData<string>(session, "familyName", out var familyName))
      {
        await SendErrorAndClearStateAsync(
          botClient,
          message.Chat.Id,
          session,
          BotConstants.Errors.SessionErrorRetry,
          cancellationToken);
        return;
      }

      // Validate detected timezone
      if (!timeZoneService.IsValidTimeZone(detectedTimezone))
      {
        await botClient.SendTextMessageAsync(
          message.Chat.Id,
          BotConstants.Errors.TimezoneValidationFailed +
          BotConstants.Errors.ChooseTimezoneManually,
          replyMarkup: new ReplyKeyboardRemove(),
          cancellationToken: cancellationToken);

        await HandleBackToTimezoneSelectionAsync(botClient, message, session, cancellationToken);
        return;
      }

      // Create family with detected timezone
      var createFamilyCommand = new CreateFamilyCommand(userId, familyName, detectedTimezone);
      var result = await Mediator.Send(createFamilyCommand, cancellationToken);

      if (!result.IsSuccess)
      {
        await SendErrorAndClearStateAsync(
          botClient,
          message.Chat.Id,
          session,
          BotConstants.Errors.FamilyCreationError(result.Errors.FirstOrDefault()),
          cancellationToken);
        return;
      }

      session.CurrentFamilyId = result.Value;
      session.ClearState();

      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        BotConstants.Messages.FamilyCreatedWithTimezone(familyName, detectedTimezone),
        parseMode: ParseMode.Markdown,
        replyMarkup: new ReplyKeyboardRemove(),
        cancellationToken: cancellationToken);
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "Error determining timezone from location");

      await botClient.SendTextMessageAsync(
        message.Chat.Id,
        BotConstants.Errors.LocationError +
        BotConstants.Errors.TryAgainOrChooseTimezone,
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

    var familyName = session.Data.TryGetValue("familyName", out var nameObj) && nameObj is string name
      ? name
      : "ваша семья";

    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      BotConstants.Messages.ChooseTimezoneMethod(familyName),
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
