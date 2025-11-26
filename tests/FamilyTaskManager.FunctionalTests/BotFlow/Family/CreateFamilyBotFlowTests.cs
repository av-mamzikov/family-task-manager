using FamilyTaskManager.FunctionalTests.Helpers;

namespace FamilyTaskManager.FunctionalTests.BotFlow.Family;

/// <summary>
///   Bot flow tests for family creation scenarios
///   Based on TEST_SCENARIOS_BOT_FLOW.md: TS-BOT-001, TS-BOT-002, TS-BOT-003
/// </summary>
public class CreateFamilyBotFlowTests : IClassFixture<CustomWebApplicationFactory<Program>>, IAsyncLifetime
{
  private readonly CustomWebApplicationFactory<Program> _factory;

  public CreateFamilyBotFlowTests(CustomWebApplicationFactory<Program> factory)
  {
    _factory = factory;
  }

  public Task InitializeAsync()
  {
    _factory.CreateClient();
    return Task.CompletedTask;
  }

  public Task DisposeAsync() => Task.CompletedTask;

  [Fact]
  public async Task TS_BOT_001_FirstStart_ShouldRegisterUserAndShowWelcome()
  {
    // Arrange
    var userId = TestDataBuilder.GenerateTelegramId();
    var chatId = userId; // In private chats, chatId = userId
    var botClient = _factory.TelegramBotClient;
    botClient.Clear();


    // Act - Send /start
    botClient.EnqueueUpdate(UpdateFactory.CreateTextUpdate(chatId, userId, "/start"));

    // Небольшое ожидание обработки апдейта хендлером бота
    await Task.Delay(1000);

    // Assert - Check bot response
    var response = botClient.GetLastMessageTo(chatId)!;
    response.ShouldContainText("Добро пожаловать");

    var keyboard = response.ShouldHaveInlineKeyboard();
    keyboard.ShouldContainButton("Создать семью");
  }

  [Fact]
  public async Task TS_BOT_002_CreateFirstFamily_ShouldCompleteFullConversation()
  {
    // Arrange
    var userId = TestDataBuilder.GenerateTelegramId();
    var chatId = userId;
    var botClient = _factory.TelegramBotClient;
    botClient.Clear();

    // Act & Assert - Step 1: Click "Create Family"
    var createFamilyCallback = UpdateFactory.CreateCallbackUpdate(chatId, userId, "create_family");
    botClient.EnqueueUpdate(createFamilyCallback);

    await Task.Delay(200);

    var response1 = botClient.GetLastMessageTo(chatId)!;
    response1.ShouldContainText("Введите название семьи");

    // Act & Assert - Step 2: Enter family name
    var nameUpdate = UpdateFactory.CreateTextUpdate(chatId, userId, "Семья Ивановых");
    botClient.EnqueueUpdate(nameUpdate);

    await Task.Delay(200);

    var response2 = botClient.GetLastMessageTo(chatId)!;
    response2.ShouldContainText("временную зону");

    // Act & Assert - Step 3: Select timezone
    var timezoneCallback = UpdateFactory.CreateCallbackUpdate(chatId, userId, "tz:Europe/Moscow");
    botClient.EnqueueUpdate(timezoneCallback);

    await Task.Delay(200);

    var finalResponse = botClient.GetLastMessageTo(chatId)!;
    finalResponse.ShouldContainText("Семья создана");
    finalResponse.ShouldContainText("Семья Ивановых");
  }

  [Fact]
  public async Task TS_BOT_003_CreateFamilyWithInvalidName_ShouldShowValidationError()
  {
    // Arrange
    var userId = TestDataBuilder.GenerateTelegramId();
    var chatId = userId;
    var botClient = _factory.TelegramBotClient;
    botClient.Clear();

    // Act - Start family creation and enter invalid name
    var createFamilyCallback = UpdateFactory.CreateCallbackUpdate(chatId, userId, "create_family");
    botClient.EnqueueUpdate(createFamilyCallback);

    var invalidNameUpdate = UpdateFactory.CreateTextUpdate(chatId, userId, "Аб"); // < 3 chars
    botClient.EnqueueUpdate(invalidNameUpdate);

    await Task.Delay(200);

    // Assert - Check bot response
    var response = botClient.GetLastMessageTo(chatId)!;
    response.ShouldContainText("ошибка");
    response.ShouldContainText("название"); // or similar validation message
  }

  [Fact]
  public async Task TS_BOT_004_SelectTimezoneByGeolocation_ShouldDetermineTimezone()
  {
    // Arrange
    var userId = TestDataBuilder.GenerateTelegramId();
    var chatId = userId;
    var botClient = _factory.TelegramBotClient;
    botClient.Clear();

    // Act - Start family creation, enter name, select geolocation option
    var createFamilyCallback = UpdateFactory.CreateCallbackUpdate(chatId, userId, "create_family");
    var nameUpdate = UpdateFactory.CreateTextUpdate(chatId, userId, "Test Family");
    var geolocationOption = UpdateFactory.CreateCallbackUpdate(chatId, userId, "tz:geolocation");

    // Send geolocation (Moscow coordinates)
    var locationUpdate = UpdateFactory.CreateLocationUpdate(chatId, userId, 55.7558, 37.6173);
    botClient.EnqueueUpdates(new[]
    {
      createFamilyCallback,
      nameUpdate,
      geolocationOption,
      locationUpdate
    });

    await Task.Delay(500);

    var finalResponse = botClient.GetLastMessageTo(chatId)!;
    finalResponse.ShouldContainText("Семья создана");
    finalResponse.ShouldContainText("Test Family");
  }
}
