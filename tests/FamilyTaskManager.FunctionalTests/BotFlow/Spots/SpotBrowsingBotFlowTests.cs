using FamilyTaskManager.FunctionalTests.Helpers;

namespace FamilyTaskManager.FunctionalTests.BotFlow.Spots;

public class SpotBrowsingBotFlowTests(CustomWebApplicationFactory<Program> factory)
  : IClassFixture<CustomWebApplicationFactory<Program>>, IAsyncLifetime
{
  public Task InitializeAsync()
  {
    factory.CreateClient();
    return Task.CompletedTask;
  }

  public Task DisposeAsync() => Task.CompletedTask;

  [Fact]
  public async Task TS_BOT_SPOT_001_ViewSpotList_ShouldShowEmptyListWithCreateButton()
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    // Arrange: Admin creates family via bot flow
    var (familyName, adminTelegramId, adminChatId) =
      await BotFamilyFlowHelpers.CreateFamilyByGeolocationAsync(factory, "Семья Ивановых");

    // Act: Navigate to spots menu
    botClient.EnqueueUpdate(UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "🧩 Споты"));

    var spotListMessage = await botClient.WaitForLastMessageAsync(adminChatId);

    // Assert
    spotListMessage.ShouldNotBeNull("Бот должен показать список спотов");
    spotListMessage!.ShouldContainText("У вас пока нет спотов");
    var keyboard = spotListMessage.ShouldHaveInlineKeyboard();
    keyboard.ShouldContainButton("➕ Создать спота");
  }

  [Fact]
  public async Task TS_BOT_SPOT_002_CreateAndViewSpot_ShouldShowSpotDetails()
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    // Arrange: Admin creates family via bot flow
    var (familyName, adminTelegramId, adminChatId) =
      await BotFamilyFlowHelpers.CreateFamilyByGeolocationAsync(factory, "Семья Петровых");

    // Navigate to spots menu
    botClient.EnqueueUpdate(UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "🧩 Споты"));
    var spotListMessage = await botClient.WaitForLastMessageAsync(adminChatId);
    spotListMessage.ShouldNotBeNull();

    var keyboard = spotListMessage!.ShouldHaveInlineKeyboard();
    var createButton = keyboard.GetButton("➕ Создать спота");
    createButton.CallbackData.ShouldNotBeNull();

    // Step 1: Click "Create spot" button
    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, createButton.CallbackData!));

    // Step 2: Select spot type (e.g., Dog)
    var spotTypeMessage = await botClient.WaitForLastMessageAsync(adminChatId);
    spotTypeMessage.ShouldNotBeNull("Бот должен показать выбор типа спота");
    spotTypeMessage!.ShouldContainText("Выберите тип спота");

    var spotTypeKeyboard = spotTypeMessage.ShouldHaveInlineKeyboard();
    var dogButton = spotTypeKeyboard.GetButton("🐶 Собака");
    dogButton.CallbackData.ShouldNotBeNull();

    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, dogButton.CallbackData!));

    // Step 3: Enter spot name
    var namePromptMessage = await botClient.WaitForLastMessageAsync(adminChatId);
    namePromptMessage.ShouldNotBeNull("Бот должен запросить имя спота");
    namePromptMessage!.ShouldContainText("Введите имя");

    botClient.EnqueueUpdate(UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "Рекс"));

    // Step 4: Wait for spot creation confirmation
    var confirmationMessage = await botClient.WaitForLastMessageAsync(adminChatId);
    confirmationMessage.ShouldNotBeNull("Бот должен подтвердить создание спота");
    confirmationMessage!.ShouldContainText("✅ Спот 🐶 \"Рекс\" успешно создан!");

    // Step 5: Navigate back to spots list
    botClient.Clear();
    botClient.EnqueueUpdate(UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "🧩 Споты"));

    var updatedSpotListMessage = await botClient.WaitForLastMessageAsync(adminChatId);
    updatedSpotListMessage.ShouldNotBeNull("Бот должен показать обновленный список спотов");
    updatedSpotListMessage!.ShouldContainText("Ваши споты");
    updatedSpotListMessage.ShouldContainText("Рекс");

    var updatedKeyboard = updatedSpotListMessage.ShouldHaveInlineKeyboard();
    var spotButton = updatedKeyboard.GetButton("🐶 Рекс");
    spotButton.CallbackData.ShouldNotBeNull();

    // Step 6: View spot details
    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, spotButton.CallbackData!));

    var spotDetailsMessage = await botClient.WaitForLastMessageAsync(adminChatId);
    spotDetailsMessage.ShouldNotBeNull("Бот должен показать детали спота");
    spotDetailsMessage!.ShouldContainText("🐶 *Рекс*");
    spotDetailsMessage.ShouldContainText("Настроение");

    var detailsKeyboard = spotDetailsMessage.ShouldHaveInlineKeyboard();
    detailsKeyboard.ShouldContainButton("📋 Шаблоны задач");
    detailsKeyboard.ShouldContainButton("🗑️ Удалить спота");
    detailsKeyboard.ShouldContainButton("⬅️ Назад к списку");
  }

  [Fact]
  public async Task TS_BOT_SPOT_003_DeleteSpot_ShouldConfirmAndRemoveSpot()
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    // Arrange: Admin creates family and spot
    var (familyName, adminTelegramId, adminChatId) =
      await BotFamilyFlowHelpers.CreateFamilyByGeolocationAsync(factory, "Семья Сидоровых");

    // Create a spot first
    botClient.EnqueueUpdates([
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "🧩 Споты")
    ]);

    var spotListMessage = await botClient.WaitForLastMessageAsync(adminChatId);
    var keyboard = spotListMessage!.ShouldHaveInlineKeyboard();
    var createButton = keyboard.GetButton("➕ Создать спота");

    botClient.EnqueueUpdates([
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, createButton.CallbackData!)
    ]);

    var spotTypeMessage = await botClient.WaitForLastMessageAsync(adminChatId);
    var spotTypeKeyboard = spotTypeMessage!.ShouldHaveInlineKeyboard();
    var catButton = spotTypeKeyboard.GetButton("🐱 Кот");

    botClient.EnqueueUpdates([
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, catButton.CallbackData!),
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "Мурка")
    ]);

    await botClient.WaitForLastMessageAsync(adminChatId);

    // Navigate to spot details
    botClient.Clear();
    botClient.EnqueueUpdate(UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "🧩 Споты"));

    var spotsMessage = await botClient.WaitForLastMessageAsync(adminChatId);
    var spotsKeyboard = spotsMessage!.ShouldHaveInlineKeyboard();
    var spotButton = spotsKeyboard.GetButton("🐱 Мурка");

    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, spotButton.CallbackData!));

    var spotDetailsMessage = await botClient.WaitForLastMessageAsync(adminChatId);
    var detailsKeyboard = spotDetailsMessage!.ShouldHaveInlineKeyboard();
    var deleteButton = detailsKeyboard.GetButton("🗑️ Удалить спота");
    deleteButton.CallbackData.ShouldNotBeNull();

    // Act: Click delete button
    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, deleteButton.CallbackData!));

    // Assert: Confirmation dialog appears
    var confirmationMessage = await botClient.WaitForLastMessageAsync(adminChatId);
    confirmationMessage.ShouldNotBeNull("Бот должен показать подтверждение удаления");
    confirmationMessage!.ShouldContainText("Удаление спота");
    confirmationMessage.ShouldContainText("Мурка");
    confirmationMessage.ShouldContainText("Внимание!");

    var confirmationKeyboard = confirmationMessage.ShouldHaveInlineKeyboard();
    var confirmButton = confirmationKeyboard.GetButton("✅ Да, удалить спота");
    confirmButton.CallbackData.ShouldNotBeNull();

    // Act: Confirm deletion
    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, confirmButton.CallbackData!));

    // Assert: Deletion success message
    var successMessage = await botClient.WaitForLastMessageAsync(adminChatId);
    successMessage.ShouldNotBeNull("Бот должен подтвердить удаление спота");
    successMessage!.ShouldContainText("✅ Спот успешно удалён");

    // Verify spot is removed from list
    botClient.Clear();
    botClient.EnqueueUpdate(UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "🧩 Споты"));

    var finalSpotListMessage = await botClient.WaitForLastMessageAsync(adminChatId);
    finalSpotListMessage.ShouldNotBeNull();
    finalSpotListMessage!.ShouldContainText("У вас пока нет спотов");
  }

  [Fact]
  public async Task TS_BOT_SPOT_004_CancelDelete_ShouldReturnToSpotList()
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    // Arrange: Admin creates family and spot
    var (familyName, adminTelegramId, adminChatId) =
      await BotFamilyFlowHelpers.CreateFamilyByGeolocationAsync(factory, "Семья Тестовых");

    // Create a spot
    botClient.EnqueueUpdates([
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "🧩 Споты")
    ]);

    var spotListMessage = await botClient.WaitForLastMessageAsync(adminChatId);
    var keyboard = spotListMessage!.ShouldHaveInlineKeyboard();
    var createButton = keyboard.GetButton("➕ Создать спота");

    botClient.EnqueueUpdates([
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, createButton.CallbackData!)
    ]);

    var spotTypeMessage = await botClient.WaitForLastMessageAsync(adminChatId);
    var spotTypeKeyboard = spotTypeMessage!.ShouldHaveInlineKeyboard();
    var plantButton = spotTypeKeyboard.GetButton("🪴 Растение");

    botClient.EnqueueUpdates([
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, plantButton.CallbackData!),
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "Фикус")
    ]);

    await botClient.WaitForLastMessageAsync(adminChatId);

    // Navigate to spot details and click delete
    botClient.Clear();
    botClient.EnqueueUpdate(UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "🧩 Споты"));

    var spotsMessage = await botClient.WaitForLastMessageAsync(adminChatId);
    var spotsKeyboard = spotsMessage!.ShouldHaveInlineKeyboard();
    var spotButton = spotsKeyboard.GetButton("Фикус");

    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, spotButton.CallbackData!));

    var spotDetailsMessage = await botClient.WaitForLastMessageAsync(adminChatId);
    var detailsKeyboard = spotDetailsMessage!.ShouldHaveInlineKeyboard();
    var deleteButton = detailsKeyboard.GetButton("🗑️ Удалить спота");

    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, deleteButton.CallbackData!));

    var confirmationMessage = await botClient.WaitForLastMessageAsync(adminChatId);
    var confirmationKeyboard = confirmationMessage!.ShouldHaveInlineKeyboard();
    var cancelButton = confirmationKeyboard.GetButton("❌ Отмена");
    cancelButton.CallbackData.ShouldNotBeNull();

    // Act: Cancel deletion
    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, cancelButton.CallbackData!));

    // Assert: Return to spot list
    var spotListAfterCancel = await botClient.WaitForLastMessageAsync(adminChatId);
    spotListAfterCancel.ShouldNotBeNull("Бот должен вернуться к списку спотов");
    spotListAfterCancel!.ShouldContainText("Ваши споты");
    spotListAfterCancel.ShouldContainText("Фикус");
  }
}
