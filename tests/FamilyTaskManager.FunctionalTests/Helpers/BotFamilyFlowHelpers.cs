using FamilyTaskManager.Host.Modules.Bot.Constants;

namespace FamilyTaskManager.FunctionalTests.Helpers;

public static class BotFamilyFlowHelpers
{
  public static async Task<(string FamilyName, long AdminTelegramId, long AdminChatId)>
    CreateFamilyByGeolocationAsync(
      CustomWebApplicationFactory<Program> factory,
      string? familyName = null,
      double latitude = 55.7558,
      double longitude = 37.6173)
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    var adminTelegramId = TestDataBuilder.GenerateTelegramId();
    var adminChatId = adminTelegramId;
    var actualFamilyName = familyName ?? "Test Family";

    botClient.EnqueueUpdates([
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, CallbackData.Family.Create()),
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, actualFamilyName),
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, CallbackData.FamilyCreation.DetectTimezone()),
      UpdateFactory.CreateLocationUpdate(adminChatId, adminTelegramId, latitude, longitude)
    ]);

    await botClient.WaitForMessagesUntilAsync(adminChatId, m => m.Text!.Contains("Главное меню"));

    return (actualFamilyName, adminTelegramId, adminChatId);
  }
}
