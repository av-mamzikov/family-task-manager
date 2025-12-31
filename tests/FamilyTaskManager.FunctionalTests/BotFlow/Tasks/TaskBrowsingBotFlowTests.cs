using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.FunctionalTests.Helpers;
using FamilyTaskManager.Host;
using FamilyTaskManager.Host.Modules.Bot.Constants;
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

  [RetryFact(3)]
  public async Task TS_BOT_TASK_001_ViewTaskList_ShouldShowNoTasksMessage()
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    // Arrange: Admin creates family via bot flow
    var (familyName, adminTelegramId, adminChatId) =
      await BotFamilyFlowHelpers.CreateFamilyByGeolocationAsync(factory, "Семья Ивановых");

    // Act: Navigate to tasks menu
    var taskListMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "✅ Мои миссии"),
      adminChatId);

    // Assert
    taskListMessage.ShouldNotBeNull("Бот должен показать список задач");
    taskListMessage!.ShouldContainText("Активных задач пока нет");
  }

  [RetryFact(3)]
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
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "✅ Мои миссии"),
      adminChatId);

    taskListMessage.ShouldNotBeNull("Бот должен показать список миссий");
    taskListMessage!.ShouldContainText("Мои миссии");
    taskListMessage.ShouldContainText("Доступные миссии");

    var taskKeyboard = taskListMessage.ShouldHaveInlineKeyboard();
    var takeTaskButton = taskKeyboard.GetButton("✋");

    var taskTakenMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, takeTaskButton.CallbackData!),
      adminChatId);

    // Assert
    taskTakenMessage.ShouldNotBeNull("Бот должен подтвердить взятие миссии");
    taskTakenMessage!.ShouldContainText("Миссия выполняется");
    var actionKeyboard = taskTakenMessage.ShouldHaveInlineKeyboard();
    actionKeyboard.ShouldContainButton("✅");
    actionKeyboard.ShouldContainButton("❌");
  }

  [RetryFact(3)]
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
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "✅ Мои миссии"),
      adminChatId);
    var takeTaskButton = taskListMessage!.ShouldHaveInlineKeyboard().GetButton("✋");

    var taskTakenMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, takeTaskButton.CallbackData!),
      adminChatId);

    // Act: Complete the task
    var completeButton = taskTakenMessage!.ShouldHaveInlineKeyboard().GetButton("✅");
    var completionMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, completeButton.CallbackData!),
      adminChatId);

    // Assert
    completionMessage.ShouldNotBeNull("Бот должен подтвердить выполнение задачи");
    completionMessage!.ShouldContainText("Миссия выполнена");
    completionMessage.ShouldContainText("Очки начислены");
  }

  [RetryFact(3)]
  public async Task TS_BOT_TASK_004_RefuseTask_ShouldReturnTaskToAvailable()
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
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "✅ Мои миссии"),
      adminChatId);
    var takeTaskButton = taskListMessage!.ShouldHaveInlineKeyboard().GetButton("✋");

    var taskTakenMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, takeTaskButton.CallbackData!),
      adminChatId);

    // Act: Refuse the task
    var refuseButton = taskTakenMessage!.ShouldHaveInlineKeyboard().GetButton("❌");
    var refuseMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, refuseButton.CallbackData!),
      adminChatId);

    // Assert
    refuseMessage.ShouldNotBeNull("Бот должен подтвердить отказ от миссии");
    refuseMessage!.ShouldContainText("Вы отказались от миссии");
    refuseMessage.ShouldContainText("Миссия снова доступна");
  }

  [RetryFact(3)]
  public async Task TS_BOT_TASK_004_DeleteTask_ShouldReturnTaskToAvailable()
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
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "✅ Мои миссии"),
      adminChatId);
    var takeTaskButton = taskListMessage!.ShouldHaveInlineKeyboard().GetButton("✋");

    var taskTakenMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, takeTaskButton.CallbackData!),
      adminChatId);

    // Act: Refuse the task
    var deleteButton = taskTakenMessage!.ShouldHaveInlineKeyboard().GetButton("🗑️ Удалить");
    var deleteMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, deleteButton.CallbackData!),
      adminChatId);

    // Assert
    deleteMessage.ShouldNotBeNull("Бот должен подтвердить отказ от задачи");
    deleteMessage!.ShouldContainText("Вы удалили миссию");
  }

  [RetryFact(3)]
  public async Task TS_BOT_TASK_005_ViewOtherTasks_ShouldShowTasksTakenByOthers_WithoutActionButtons()
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    // Arrange: Create family and spot
    var (familyName, adminTelegramId, adminChatId) =
      await BotFamilyFlowHelpers.CreateFamilyByGeolocationAsync(factory, "Семья для других задач");

    await CreateSpotAsync(botClient, adminChatId, adminTelegramId, "🐶 Собака", "Рекс");

    // Create two tasks so that admin still has at least one task in "My tasks" screen
    await CreateTaskFromSpotTemplateAsync(botClient, adminChatId, adminTelegramId, "🐶 Рекс");
    await CreateTaskFromSpotTemplateAsync(botClient, adminChatId, adminTelegramId, "🐶 Рекс");

    // Add second member and let them take one task
    var otherTelegramId = await BotFamilyFlowHelpers.AddFamilyMemberViaInviteAsync(
      botClient,
      adminChatId,
      adminTelegramId,
      FamilyRole.Adult,
      "Другой участник");
    var otherChatId = otherTelegramId;

    botClient.Clear();
    var otherTaskListMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(otherChatId, otherTelegramId, "✅ Мои миссии"),
      otherChatId);
    otherTaskListMessage.ShouldNotBeNull("Другой участник должен увидеть список задач");
    var otherTaskKeyboard = otherTaskListMessage!.ShouldHaveInlineKeyboard();
    var otherTakeButton = otherTaskKeyboard.GetButton("✋");

    var takenTaskTitle = otherTakeButton.Text.Replace("✋", string.Empty).Trim().Split(": ").Skip(1).First();
    takenTaskTitle.ShouldNotBeNullOrWhiteSpace();

    await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(otherChatId, otherTelegramId, otherTakeButton.CallbackData!),
      otherChatId);

    // Act: Admin opens "Other tasks" list (invoke callback directly to avoid dependency on MyTasks keyboard)
    botClient.Clear();
    var otherTasksMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, CallbackData.TaskBrowsing.OtherList()),
      adminChatId);

    // Assert: Other tasks list shows taken task and has no action buttons
    otherTasksMessage.ShouldNotBeNull("Админ должен увидеть список других миссий");
    otherTasksMessage!.ShouldContainText("Другие миссии");
    otherTasksMessage.ShouldContainText(takenTaskTitle);

    var otherTasksKeyboard = otherTasksMessage.ShouldHaveInlineKeyboard();
    otherTasksKeyboard.ShouldContainButton("⬅️ Назад");
    otherTasksKeyboard.ShouldContainButton("✋"); // Специально сделана возможность отобрать задачу
    otherTasksKeyboard.ShouldNotContainButton("✅");
    otherTasksKeyboard.ShouldNotContainButton("❌");
    otherTasksKeyboard.ShouldNotContainButton("🗑️");
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
