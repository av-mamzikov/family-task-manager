using FamilyTaskManager.FunctionalTests.Helpers;
using FamilyTaskManager.Host;
using Telegram.Bot.Types;

namespace FamilyTaskManager.FunctionalTests.BotFlow.Tasks;

public class TaskBrowsingBotFlowTests(CustomWebApplicationFactory<Program> factory)
  : IClassFixture<CustomWebApplicationFactory<Program>>, IAsyncLifetime
{
  public Task InitializeAsync()
  {
    factory.CreateClient();
    return Task.CompletedTask;
  }

  public Task DisposeAsync() => Task.CompletedTask;

  [Fact]
  public async Task TS_BOT_TASK_001_ViewTaskList_ShouldShowNoTasksMessage()
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    // Arrange: Admin creates family via bot flow
    var (familyName, adminTelegramId, adminChatId) =
      await BotFamilyFlowHelpers.CreateFamilyByGeolocationAsync(factory, "Семья Ивановых");

    // Act: Navigate to tasks menu
    var taskListMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "✅ Наши задачи"),
      adminChatId);

    // Assert
    taskListMessage.ShouldNotBeNull("Бот должен показать список задач");
    taskListMessage!.ShouldContainText("Активных задач пока нет");
  }

  [Fact]
  public async Task TS_BOT_TASK_002_TakeTask_ShouldShowTaskInProgress()
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    // Arrange: Create family and spot with templates
    var (familyName, adminTelegramId, adminChatId) =
      await BotFamilyFlowHelpers.CreateFamilyByGeolocationAsync(factory, "Семья Петровых");

    // Create spot (which auto-creates templates)
    await CreateSpotAsync(botClient, adminChatId, adminTelegramId, "🐶 Собака", "Рекс");

    // Navigate to spot templates and create a task
    botClient.Clear();
    var spotsMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "🧩 Споты"),
      adminChatId);
    var spotButton = spotsMessage!.ShouldHaveInlineKeyboard().GetButton("🐶 Рекс");

    var spotDetailsMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, spotButton.CallbackData!),
      adminChatId);
    var templatesButton = spotDetailsMessage!.ShouldHaveInlineKeyboard().GetButton("📋 Шаблоны задач");

    var templatesMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, templatesButton.CallbackData!),
      adminChatId);
    var templateKeyboard = templatesMessage!.ShouldHaveInlineKeyboard();
    var firstTemplateButton = templateKeyboard.InlineKeyboard.First().First();

    // View template details
    var templateDetailsMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, firstTemplateButton.CallbackData!),
      adminChatId);
    var createTaskButton = templateDetailsMessage!.ShouldHaveInlineKeyboard().GetButton("Создать задачу сейчас");

    // Create task from template
    var taskCreatedMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, createTaskButton.CallbackData!),
      adminChatId);
    taskCreatedMessage.ShouldNotBeNull();
    taskCreatedMessage!.ShouldContainText("Задача создана");

    // Act: Navigate to tasks and take the task
    botClient.Clear();
    var taskListMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "✅ Наши задачи"),
      adminChatId);

    taskListMessage.ShouldNotBeNull("Бот должен показать список задач");
    taskListMessage!.ShouldContainText("Наши задачи");
    taskListMessage.ShouldContainText("Доступные задачи");

    var taskKeyboard = taskListMessage.ShouldHaveInlineKeyboard();
    var takeTaskButton = taskKeyboard.InlineKeyboard.First().First();
    takeTaskButton.Text.ShouldContain("✋ Взять");

    var taskTakenMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, takeTaskButton.CallbackData!),
      adminChatId);

    // Assert
    taskTakenMessage.ShouldNotBeNull("Бот должен подтвердить взятие задачи");
    taskTakenMessage!.ShouldContainText("Задача взята в работу");
    var actionKeyboard = taskTakenMessage.ShouldHaveInlineKeyboard();
    actionKeyboard.ShouldContainButton("✅ Выполнить");
    actionKeyboard.ShouldContainButton("❌ Отказаться");
  }

  [Fact]
  public async Task TS_BOT_TASK_003_CompleteTask_ShouldShowSuccessMessage()
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    // Arrange: Create family, spot, and task
    var (familyName, adminTelegramId, adminChatId) =
      await BotFamilyFlowHelpers.CreateFamilyByGeolocationAsync(factory, "Семья Сидоровых");

    await CreateSpotAsync(botClient, adminChatId, adminTelegramId, "🐱 Кот", "Мурка");
    await CreateTaskFromSpotTemplateAsync(botClient, adminChatId, adminTelegramId, "🐱 Мурка");

    // Take the task
    botClient.Clear();
    var taskListMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "✅ Наши задачи"),
      adminChatId);
    var takeTaskButton = taskListMessage!.ShouldHaveInlineKeyboard().InlineKeyboard.First().First();

    var taskTakenMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, takeTaskButton.CallbackData!),
      adminChatId);

    // Act: Complete the task
    var completeButton = taskTakenMessage!.ShouldHaveInlineKeyboard().GetButton("✅ Выполнить");
    var completionMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, completeButton.CallbackData!),
      adminChatId);

    // Assert
    completionMessage.ShouldNotBeNull("Бот должен подтвердить выполнение задачи");
    completionMessage!.ShouldContainText("Задача выполнена");
    completionMessage.ShouldContainText("Очки начислены");
  }

  [Fact]
  public async Task TS_BOT_TASK_004_CancelTask_ShouldReturnTaskToAvailable()
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    // Arrange: Create family, spot, and task
    var (familyName, adminTelegramId, adminChatId) =
      await BotFamilyFlowHelpers.CreateFamilyByGeolocationAsync(factory, "Семья Тестовых");

    await CreateSpotAsync(botClient, adminChatId, adminTelegramId, "🪴 Растение", "Фикус");
    await CreateTaskFromSpotTemplateAsync(botClient, adminChatId, adminTelegramId, "🪴 Фикус");

    // Take the task
    botClient.Clear();
    var taskListMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "✅ Наши задачи"),
      adminChatId);
    var takeTaskButton = taskListMessage!.ShouldHaveInlineKeyboard().InlineKeyboard.First().First();

    var taskTakenMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, takeTaskButton.CallbackData!),
      adminChatId);

    // Act: Refuse the task
    var cancelButton = taskTakenMessage!.ShouldHaveInlineKeyboard().GetButton("❌ Отказаться");
    var cancelMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, cancelButton.CallbackData!),
      adminChatId);

    // Assert
    cancelMessage.ShouldNotBeNull("Бот должен подтвердить отказ от задачи");
    cancelMessage!.ShouldContainText("Вы отказались от задачи");
    cancelMessage.ShouldContainText("Задача снова доступна");
  }

  private async Task CreateSpotAsync(dynamic botClient, long chatId, long telegramId,
    string spotType, string spotName)
  {
    Message spotListMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(chatId, telegramId, "🧩 Споты"),
      chatId);
    var createButton = spotListMessage!.ShouldHaveInlineKeyboard().GetButton("➕ Создать спота");

    Message spotTypeMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(chatId, telegramId, createButton.CallbackData!),
      chatId);
    var typeButton = spotTypeMessage!.ShouldHaveInlineKeyboard().GetButton(spotType);

    await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(chatId, telegramId, typeButton.CallbackData!),
      chatId);

    await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(chatId, telegramId, spotName),
      chatId);
  }

  private async Task CreateTaskFromSpotTemplateAsync(dynamic botClient, long chatId, long telegramId,
    string spotButtonText)
  {
    botClient.Clear();
    Message spotsMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(chatId, telegramId, "🧩 Споты"),
      chatId);
    var spotButton = spotsMessage!.ShouldHaveInlineKeyboard().GetButton(spotButtonText);

    Message spotDetailsMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(chatId, telegramId, spotButton.CallbackData!),
      chatId);
    var templatesButton = spotDetailsMessage!.ShouldHaveInlineKeyboard().GetButton("📋 Шаблоны задач");

    Message templatesMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(chatId, telegramId, templatesButton.CallbackData!),
      chatId);
    var firstTemplateButton = templatesMessage!.ShouldHaveInlineKeyboard().InlineKeyboard.First().First();

    Message templateDetailsMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(chatId, telegramId, firstTemplateButton.CallbackData!),
      chatId);
    var createTaskButton = templateDetailsMessage!.ShouldHaveInlineKeyboard().GetButton("Создать задачу сейчас");

    await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(chatId, telegramId, createTaskButton.CallbackData!),
      chatId);
  }
}
