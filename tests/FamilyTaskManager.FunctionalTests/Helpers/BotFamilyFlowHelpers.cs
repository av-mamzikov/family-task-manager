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

    botClient.EnqueueUpdates(new[]
    {
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, "create_family"),
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, actualFamilyName),
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, "timezone_detect"),
      UpdateFactory.CreateLocationUpdate(adminChatId, adminTelegramId, latitude, longitude)
    });

    await botClient.WaitForMessagesUntilAsync(adminChatId, m => m.Text!.Contains("Главное меню"));

    return (actualFamilyName, adminTelegramId, adminChatId);
  }
}
