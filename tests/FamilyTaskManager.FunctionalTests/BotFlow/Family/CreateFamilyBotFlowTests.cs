using FamilyTaskManager.FunctionalTests.Helpers;
using FamilyTaskManager.Host;
using FamilyTaskManager.Host.Modules.Bot.Constants;
using FamilyTaskManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyTaskManager.FunctionalTests.BotFlow.Family;

/// <summary>
///   Bot flow tests for family creation scenarios
///   Based on TEST_SCENARIOS_BOT_FLOW.md: TS-BOT-001, TS-BOT-002, TS-BOT-003
/// </summary>
public class CreateFamilyBotFlowTests(CustomWebApplicationFactory<Program> factory)
  : IClassFixture<CustomWebApplicationFactory<Program>>, IAsyncLifetime
{
  public Task InitializeAsync()
  {
    factory.CreateClient();
    return Task.CompletedTask;
  }

  public Task DisposeAsync() => Task.CompletedTask;

  [RetryFact(3)]
  public async Task TS_BOT_001_FirstStart_ShouldRegisterUserAndShowWelcome()
  {
    var chatId = TestDataBuilder.GenerateTelegramId();
    var userId = TestDataBuilder.GenerateTelegramId();
    // Arrange
    var botClient = factory.TelegramBotClient;
    botClient.Clear();


    // Act - Send /start and wait for welcome message
    var response = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(chatId, userId, "/start"),
      chatId);
    response.ShouldNotBeNull("Бот должен отправить приветственное сообщение при первом запуске");
    response!.ShouldContainText("Добро пожаловать");

    var keyboard = response.ShouldHaveInlineKeyboard();
    keyboard.ShouldContainButton("Создать семью");
  }

  [RetryFact(3)]
  public async Task TS_BOT_001A_FirstStart_WithCampaign_ShouldPersistCampaignId()
  {
    var chatId = TestDataBuilder.GenerateTelegramId();
    var telegramUserId = TestDataBuilder.GenerateTelegramId();
    const string campaignId = "123";

    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    var response = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(chatId, telegramUserId, $"/start campaign_{campaignId}"),
      chatId);

    response.ShouldNotBeNull("Бот должен отправить приветственное сообщение при первом запуске");

    await using var scope = factory.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    var user = await dbContext.Users.SingleOrDefaultAsync(u => u.TelegramId == telegramUserId);
    user.ShouldNotBeNull("Пользователь должен быть создан после первого запуска бота");
    user!.CampaignId.ShouldBe(campaignId, "Идентификатор кампании должен быть сохранен");
  }

  [RetryFact(2)]
  public async Task TS_BOT_002_CreateFirstFamily_ShouldCompleteFullConversation()
  {
    // Arrange
    var userId = TestDataBuilder.GenerateTelegramId();
    var chatId = TestDataBuilder.GenerateTelegramId();
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    // Initialize user with /start command
    await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(chatId, userId, "/start"),
      chatId);

    // Act & Assert - Step 1: Click "Create Family"
    var createFamilyCallback = UpdateFactory.CreateCallbackUpdate(chatId, userId, CallbackData.Family.Create());
    var step1Messages = (await botClient.SendUpdateAndWaitForMessagesAsync(createFamilyCallback, chatId, 1)).ToList();
    var response1 = step1Messages.LastOrDefault();
    response1.ShouldNotBeNull("Бот должен попросить ввести название семьи");
    response1!.ShouldContainText("Введите название семьи");

    // Act & Assert - Step 2: Enter family name
    var nameUpdate = UpdateFactory.CreateTextUpdate(chatId, userId, "Семья Ивановых");
    var step2Messages =
      (await botClient.SendUpdateAndWaitForMessagesAsync(nameUpdate, chatId, 1)).ToList();
    var response2 = step2Messages.LastOrDefault();
    response2.ShouldNotBeNull("Бот должен попросить выбрать способ определения временной зоны");
    response2!.ShouldContainText("Выберите способ определения временной зоны");

    // Act & Assert - Step 3: Show timezone list
    var showTimezoneList =
      UpdateFactory.CreateCallbackUpdate(chatId, userId, CallbackData.FamilyCreation.ShowTimezoneList());

    var step3Messages =
      (await botClient.SendUpdateAndWaitForMessagesAsync(showTimezoneList, chatId, 1)).ToList();
    var timezonePrompt = step3Messages.LastOrDefault();
    timezonePrompt.ShouldNotBeNull("Бот должен показать список временных зон");
    timezonePrompt!.ShouldContainText("Выберите временную зону");

    // Act & Assert - Step 4: Select timezone from list
    var timezoneSelection =
      UpdateFactory.CreateCallbackUpdate(chatId, userId, CallbackData.FamilyCreation.TimeZone("Europe/Moscow"));

    var messages = (await botClient.SendUpdateAndWaitForMessagesAsync(timezoneSelection, chatId, 2))
      .ToList();
    var successMessage = messages.FirstOrDefault(m => m.Text?.Contains("Семья Ивановых") == true);
    successMessage.ShouldNotBeNull("Должно быть сообщение с подтверждением создания семьи");
    successMessage!.ShouldContainText("Москва");

    var menuMessage = messages.Last();
    menuMessage.ShouldContainText("Главное меню");

    var familyMenuMessages = (await botClient.SendUpdateAndWaitForMessagesAsync(
      UpdateFactory.CreateTextUpdate(chatId, userId, "🏠 Семья"),
      chatId,
      1)).ToList();
    var familyMenuMessage = familyMenuMessages.LastOrDefault();
    familyMenuMessage.ShouldNotBeNull("После нажатия на кнопку 'Семья' должно отображаться меню текущей семьи");
    familyMenuMessage!.ShouldContainText("Семья Ивановых");
  }

  [RetryFact(2)]
  public async Task TS_BOT_003_CreateFamilyWithInvalidName_ShouldShowValidationError()
  {
    // Arrange
    var userId = TestDataBuilder.GenerateTelegramId();
    var chatId = userId;
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    // Act - Start family creation and enter invalid name
    var response = await botClient.SendUpdateAndWaitForLastMessageAsync(
      [
        UpdateFactory.CreateCallbackUpdate(chatId, userId, CallbackData.Family.Create()),
        UpdateFactory.CreateTextUpdate(chatId, userId, "Аб") // < 3 chars
      ],
      chatId);
    response.ShouldNotBeNull("Бот должен показать ошибку валидации имени семьи");
    response!.ShouldContainText(BotMessages.Errors.FamilyNameTooShort);
  }

  [RetryFact(2)]
  public async Task TS_BOT_004_SelectTimezoneByGeolocation_ShouldDetermineTimezone()
  {
    // Arrange
    var userId = TestDataBuilder.GenerateTelegramId();
    var chatId = userId;
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    var messages = (await botClient.SendUpdateAndWaitForMessagesAsync(
      [
        UpdateFactory.CreateCallbackUpdate(chatId, userId, CallbackData.Family.Create()),
        UpdateFactory.CreateTextUpdate(chatId, userId, "Test Family"),
        UpdateFactory.CreateCallbackUpdate(chatId, userId, CallbackData.FamilyCreation.DetectTimezone()),
        UpdateFactory.CreateLocationUpdate(chatId, userId, 55.7558, 37.6173)
      ],
      chatId,
      6)).ToList();
    var successMessage = messages.LastOrDefault(m => m.Text?.Contains("успешно создана") == true);
    successMessage.ShouldNotBeNull("Должно быть сообщение с подтверждением создания семьи");
    successMessage!.ShouldContainText("Test Family");

    var menuMessage = messages.Last();
    menuMessage.ShouldContainText("Главное меню");

    var familyMenuMessages = (await botClient.SendUpdateAndWaitForMessagesAsync(
      UpdateFactory.CreateTextUpdate(chatId, userId, "🏠 Семья"),
      chatId,
      1)).ToList();
    var familyMenuMessage = familyMenuMessages.LastOrDefault();
    familyMenuMessage.ShouldNotBeNull("После нажатия на кнопку 'Семья' должно отображаться меню текущей семьи");
    familyMenuMessage!.ShouldContainText("Test Family");
  }
}
