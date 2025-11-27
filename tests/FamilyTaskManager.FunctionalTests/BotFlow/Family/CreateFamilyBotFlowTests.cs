using FamilyTaskManager.FunctionalTests.Helpers;
using FamilyTaskManager.Host.Modules.Bot;

namespace FamilyTaskManager.FunctionalTests.BotFlow.Family;

/// <summary>
///   Bot flow tests for family creation scenarios
///   Based on TEST_SCENARIOS_BOT_FLOW.md: TS-BOT-001, TS-BOT-002, TS-BOT-003
/// </summary>
public class CreateFamilyBotFlowTests(CustomWebApplicationFactory<Program> factory)
  : IClassFixture<CustomWebApplicationFactory<Program>>, IAsyncLifetime
{
  private const int BotProcessingDelayMs = 1000;

  public Task InitializeAsync()
  {
    factory.CreateClient();
    return Task.CompletedTask;
  }

  public Task DisposeAsync() => Task.CompletedTask;

  [Fact]
  public async Task TS_BOT_001_FirstStart_ShouldRegisterUserAndShowWelcome()
  {
    // Arrange
    var userId = TestDataBuilder.GenerateTelegramId();
    var chatId = userId; // In private chats, chatId = userId
    var botClient = factory.TelegramBotClient;
    botClient.Clear();


    // Act - Send /start
    botClient.EnqueueUpdate(UpdateFactory.CreateTextUpdate(chatId, userId, "/start"));

    // Небольшое ожидание обработки апдейта хендлером бота
    await Task.Delay(BotProcessingDelayMs);

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
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    // Act & Assert - Step 1: Click "Create Family"
    var createFamilyCallback = UpdateFactory.CreateCallbackUpdate(chatId, userId, "create_family");
    botClient.EnqueueUpdate(createFamilyCallback);

    await Task.Delay(BotProcessingDelayMs);

    var response1 = botClient.GetLastMessageTo(chatId)!;
    response1.ShouldContainText("Введите название семьи");

    // Act & Assert - Step 2: Enter family name
    var nameUpdate = UpdateFactory.CreateTextUpdate(chatId, userId, "Семья Ивановых");
    botClient.EnqueueUpdate(nameUpdate);

    await Task.Delay(BotProcessingDelayMs);

    var response2 = botClient.GetLastMessageTo(chatId)!;
    response2.ShouldContainText("Выберите способ определения временной зоны");

    // Act & Assert - Step 3: Show timezone list
    var showTimezoneList = UpdateFactory.CreateCallbackUpdate(chatId, userId, "timezone_showlist");
    botClient.EnqueueUpdate(showTimezoneList);

    await Task.Delay(BotProcessingDelayMs);

    var timezonePrompt = botClient.GetLastMessageTo(chatId)!;
    timezonePrompt.ShouldContainText("Выберите временную зону");

    // Act & Assert - Step 4: Select timezone from list
    var timezoneSelection = UpdateFactory.CreateCallbackUpdate(chatId, userId, "timezone_Europe/Moscow");
    botClient.EnqueueUpdate(timezoneSelection);

    await Task.Delay(BotProcessingDelayMs);

    var messages = botClient.GetMessagesTo(chatId).ToList();
    var successMessage = messages.LastOrDefault(m => m.Text?.Contains("Временная зона: Europe/Moscow") == true);
    successMessage.ShouldNotBeNull("Должно быть сообщение с подтверждением создания семьи");
    successMessage!.ShouldContainText("Семья Ивановых");

    var menuMessage = messages.Last();
    menuMessage.ShouldContainText("Главное меню");
  }

  [Fact]
  public async Task TS_BOT_003_CreateFamilyWithInvalidName_ShouldShowValidationError()
  {
    // Arrange
    var userId = TestDataBuilder.GenerateTelegramId();
    var chatId = userId;
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    // Act - Start family creation and enter invalid name
    var createFamilyCallback = UpdateFactory.CreateCallbackUpdate(chatId, userId, "create_family");
    botClient.EnqueueUpdate(createFamilyCallback);

    var invalidNameUpdate = UpdateFactory.CreateTextUpdate(chatId, userId, "Аб"); // < 3 chars
    botClient.EnqueueUpdate(invalidNameUpdate);

    await Task.Delay(BotProcessingDelayMs);

    // Assert - Check bot response
    var response = botClient.GetLastMessageTo(chatId)!;
    response.ShouldContainText(BotConstants.Errors.FamilyNameTooShort);
  }

  [Fact]
  public async Task TS_BOT_004_SelectTimezoneByGeolocation_ShouldDetermineTimezone()
  {
    // Arrange
    var userId = TestDataBuilder.GenerateTelegramId();
    var chatId = userId;
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    // Act - Start family creation, enter name, select geolocation option
    var createFamilyCallback = UpdateFactory.CreateCallbackUpdate(chatId, userId, "create_family");
    var nameUpdate = UpdateFactory.CreateTextUpdate(chatId, userId, "Test Family");
    var geolocationOption = UpdateFactory.CreateCallbackUpdate(chatId, userId, "timezone_detect");

    // Send geolocation (Moscow coordinates)
    var locationUpdate = UpdateFactory.CreateLocationUpdate(chatId, userId, 55.7558, 37.6173);
    botClient.EnqueueUpdates(new[]
    {
      createFamilyCallback,
      nameUpdate,
      geolocationOption,
      locationUpdate
    });

    await Task.Delay(BotProcessingDelayMs);

    var messages = botClient.GetMessagesTo(chatId).ToList();
    var successMessage = messages.LastOrDefault(m => m.Text?.Contains("успешно создана") == true);
    successMessage.ShouldNotBeNull("Должно быть сообщение с подтверждением создания семьи");
    successMessage!.ShouldContainText("Test Family");

    var menuMessage = messages.Last();
    menuMessage.ShouldContainText("Главное меню");
  }
}
