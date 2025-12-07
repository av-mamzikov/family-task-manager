using FamilyTaskManager.FunctionalTests.Helpers;
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
    botClient.EnqueueUpdate(UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "✅ Наши задачи"));

    var taskListMessage = await botClient.WaitForLastMessageAsync(adminChatId);

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
    botClient.EnqueueUpdate(UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "🧩 Споты"));
    var spotsMessage = await botClient.WaitForLastMessageAsync(adminChatId);
    var spotButton = spotsMessage!.ShouldHaveInlineKeyboard().GetButton("🐶 Рекс");

    botClient.EnqueueUpdate(UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, spotButton.CallbackData!));
    var spotDetailsMessage = await botClient.WaitForLastMessageAsync(adminChatId);
    var templatesButton = spotDetailsMessage!.ShouldHaveInlineKeyboard().GetButton("📋 Шаблоны задач");

    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, templatesButton.CallbackData!));
    var templatesMessage = await botClient.WaitForLastMessageAsync(adminChatId);
    var templateKeyboard = templatesMessage!.ShouldHaveInlineKeyboard();
    var firstTemplateButton = templateKeyboard.InlineKeyboard.First().First();

    // View template details
    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, firstTemplateButton.CallbackData!));
    var templateDetailsMessage = await botClient.WaitForLastMessageAsync(adminChatId);
    var createTaskButton = templateDetailsMessage!.ShouldHaveInlineKeyboard().GetButton("Создать задачу сейчас");

    // Create task from template
    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, createTaskButton.CallbackData!));
    var taskCreatedMessage = await botClient.WaitForLastMessageAsync(adminChatId);
    taskCreatedMessage.ShouldNotBeNull();
    taskCreatedMessage!.ShouldContainText("Задача создана");

    // Act: Navigate to tasks and take the task
    botClient.Clear();
    botClient.EnqueueUpdate(UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "✅ Наши задачи"));
    var taskListMessage = await botClient.WaitForLastMessageAsync(adminChatId);

    taskListMessage.ShouldNotBeNull("Бот должен показать список задач");
    taskListMessage!.ShouldContainText("Наши задачи");
    taskListMessage.ShouldContainText("Доступные задачи");

    var taskKeyboard = taskListMessage.ShouldHaveInlineKeyboard();
    var takeTaskButton = taskKeyboard.InlineKeyboard.First().First();
    takeTaskButton.Text.ShouldContain("✋ Взять");

    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, takeTaskButton.CallbackData!));

    var taskTakenMessage = await botClient.WaitForLastMessageAsync(adminChatId);

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
    botClient.EnqueueUpdate(UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "✅ Наши задачи"));
    var taskListMessage = await botClient.WaitForLastMessageAsync(adminChatId);
    var takeTaskButton = taskListMessage!.ShouldHaveInlineKeyboard().InlineKeyboard.First().First();

    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, takeTaskButton.CallbackData!));
    var taskTakenMessage = await botClient.WaitForLastMessageAsync(adminChatId);

    // Act: Complete the task
    var completeButton = taskTakenMessage!.ShouldHaveInlineKeyboard().GetButton("✅ Выполнить");
    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, completeButton.CallbackData!));

    var completionMessage = await botClient.WaitForLastMessageAsync(adminChatId);

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
    botClient.EnqueueUpdate(UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "✅ Наши задачи"));
    var taskListMessage = await botClient.WaitForLastMessageAsync(adminChatId);
    var takeTaskButton = taskListMessage!.ShouldHaveInlineKeyboard().InlineKeyboard.First().First();

    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, takeTaskButton.CallbackData!));
    var taskTakenMessage = await botClient.WaitForLastMessageAsync(adminChatId);

    // Act: Refuse the task
    var cancelButton = taskTakenMessage!.ShouldHaveInlineKeyboard().GetButton("❌ Отказаться");
    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, cancelButton.CallbackData!));

    var cancelMessage = await botClient.WaitForLastMessageAsync(adminChatId);

    // Assert
    cancelMessage.ShouldNotBeNull("Бот должен подтвердить отказ от задачи");
    cancelMessage!.ShouldContainText("Вы отказались от задачи");
    cancelMessage.ShouldContainText("Задача снова доступна");
  }

  private async Task CreateSpotAsync(dynamic botClient, long chatId, long telegramId,
    string spotType, string spotName)
  {
    botClient.EnqueueUpdate(UpdateFactory.CreateTextUpdate(chatId, telegramId, "🧩 Споты"));
    Message spotListMessage = await botClient.WaitForLastMessageAsync(chatId);
    var createButton = spotListMessage!.ShouldHaveInlineKeyboard().GetButton("➕ Создать спота");

    botClient.EnqueueUpdate(UpdateFactory.CreateCallbackUpdate(chatId, telegramId, createButton.CallbackData!));
    Message spotTypeMessage = await botClient.WaitForLastMessageAsync(chatId);
    var typeButton = spotTypeMessage!.ShouldHaveInlineKeyboard().GetButton(spotType);

    botClient.EnqueueUpdate(UpdateFactory.CreateCallbackUpdate(chatId, telegramId, typeButton.CallbackData!));
    await botClient.WaitForLastMessageAsync(chatId);

    botClient.EnqueueUpdate(UpdateFactory.CreateTextUpdate(chatId, telegramId, spotName));
    await botClient.WaitForLastMessageAsync(chatId);
  }

  private async Task CreateTaskFromSpotTemplateAsync(dynamic botClient, long chatId, long telegramId,
    string spotButtonText)
  {
    botClient.Clear();
    botClient.EnqueueUpdate(UpdateFactory.CreateTextUpdate(chatId, telegramId, "🧩 Споты"));
    Message spotsMessage = await botClient.WaitForLastMessageAsync(chatId);
    var spotButton = spotsMessage!.ShouldHaveInlineKeyboard().GetButton(spotButtonText);

    botClient.EnqueueUpdate(UpdateFactory.CreateCallbackUpdate(chatId, telegramId, spotButton.CallbackData!));
    Message spotDetailsMessage = await botClient.WaitForLastMessageAsync(chatId);
    var templatesButton = spotDetailsMessage!.ShouldHaveInlineKeyboard().GetButton("📋 Шаблоны задач");

    botClient.EnqueueUpdate(UpdateFactory.CreateCallbackUpdate(chatId, telegramId, templatesButton.CallbackData!));
    Message templatesMessage = await botClient.WaitForLastMessageAsync(chatId);
    var firstTemplateButton = templatesMessage!.ShouldHaveInlineKeyboard().InlineKeyboard.First().First();

    botClient.EnqueueUpdate(UpdateFactory.CreateCallbackUpdate(chatId, telegramId, firstTemplateButton.CallbackData!));
    Message templateDetailsMessage = await botClient.WaitForLastMessageAsync(chatId);
    var createTaskButton = templateDetailsMessage!.ShouldHaveInlineKeyboard().GetButton("Создать задачу сейчас");

    botClient.EnqueueUpdate(UpdateFactory.CreateCallbackUpdate(chatId, telegramId, createTaskButton.CallbackData!));
    await botClient.WaitForLastMessageAsync(chatId);
  }
}
