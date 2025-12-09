using FamilyTaskManager.FunctionalTests.Helpers;
using FamilyTaskManager.Host;

namespace FamilyTaskManager.FunctionalTests.BotFlow.Stats;

public class StatsBrowsingBotFlowTests(CustomWebApplicationFactory<Program> factory)
  : IClassFixture<CustomWebApplicationFactory<Program>>, IAsyncLifetime
{
  public Task InitializeAsync()
  {
    factory.CreateClient();
    return Task.CompletedTask;
  }

  public Task DisposeAsync() => Task.CompletedTask;

  [Fact]
  public async Task TS_BOT_STATS_001_ViewStats_ShouldShowLeaderboard()
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    // Arrange: Admin creates family via bot flow
    var (familyName, adminTelegramId, adminChatId) =
      await BotFamilyFlowHelpers.CreateFamilyByGeolocationAsync(factory, "Семья Ивановых");

    // Act: Navigate to stats menu
    var statsMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "📊 Статистика"),
      adminChatId);

    // Assert
    statsMessage.ShouldNotBeNull("Бот должен показать статистику семьи");
    statsMessage!.ShouldContainText("Статистика семьи");
    statsMessage.ShouldContainText("Лидерборд");
  }
}
