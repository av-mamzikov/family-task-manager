using System.Text.RegularExpressions;
using FamilyTaskManager.FunctionalTests.Helpers;
using FamilyTaskManager.Host;

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

  [RetryFact(2)]
  public async Task TS_BOT_SPOT_001_ViewSpotList_ShouldShowEmptyListWithCreateButton()
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    // Arrange: Admin creates family via bot flow
    var (familyName, adminTelegramId, adminChatId) =
      await BotFamilyFlowHelpers.CreateFamilyByGeolocationAsync(factory, "Семья Ивановых");

    // Act: Navigate to spots menu
    var spotListMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "🧩 Споты"),
      adminChatId);

    // Assert
    spotListMessage.ShouldNotBeNull("Бот должен показать список спотов");
    spotListMessage!.ShouldContainText("У вас пока нет спотов");
    var keyboard = spotListMessage.ShouldHaveInlineKeyboard();
    keyboard.ShouldContainButton("➕ Создать спота");
  }

  [RetryFact(2)]
  public async Task TS_BOT_SPOT_002_CreateAndViewSpot_ShouldShowSpotDetails()
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    // Arrange: Admin creates family via bot flow
    var (familyName, adminTelegramId, adminChatId) =
      await BotFamilyFlowHelpers.CreateFamilyByGeolocationAsync(factory, "Семья Петровых");

    // Navigate to spots menu
    var spotListMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "🧩 Споты"),
      adminChatId);
    spotListMessage.ShouldNotBeNull();

    var keyboard = spotListMessage!.ShouldHaveInlineKeyboard();
    var createButton = keyboard.GetButton("➕ Создать спота");
    createButton.CallbackData.ShouldNotBeNull();

    // Step 1: Click "Create spot" button and wait for type selection
    var spotTypeMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, createButton.CallbackData!),
      adminChatId);
    spotTypeMessage.ShouldNotBeNull("Бот должен показать выбор типа спота");
    spotTypeMessage!.ShouldContainText("Выберите тип спота");

    var spotTypeKeyboard = spotTypeMessage.ShouldHaveInlineKeyboard();
    var dogButton = spotTypeKeyboard.GetButton("🐶 Собака");
    dogButton.CallbackData.ShouldNotBeNull();

    // Step 3: Select spot type and wait for name prompt
    var namePromptMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, dogButton.CallbackData!),
      adminChatId);
    namePromptMessage.ShouldNotBeNull("Бот должен запросить имя спота");
    namePromptMessage!.ShouldContainText("Введите имя");

    // Step 4: Enter spot name and wait for confirmation
    var confirmationMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "Рекс"),
      adminChatId);
    confirmationMessage.ShouldNotBeNull("Бот должен подтвердить создание спота");
    confirmationMessage!.ShouldContainText("✅ Спот 🐶 \"Рекс\" успешно создан!");

    // Step 5: Navigate back to spots list
    botClient.Clear();
    var updatedSpotListMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "🧩 Споты"),
      adminChatId);
    updatedSpotListMessage.ShouldNotBeNull("Бот должен показать обновленный список спотов");
    updatedSpotListMessage!.ShouldContainText("Ваши споты");
    updatedSpotListMessage.ShouldContainText("Рекс");

    var updatedKeyboard = updatedSpotListMessage.ShouldHaveInlineKeyboard();
    var spotButton = updatedKeyboard.GetButton("🐶 Рекс");
    spotButton.CallbackData.ShouldNotBeNull();

    // Step 6: View spot details
    var spotDetailsMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, spotButton.CallbackData!),
      adminChatId);
    spotDetailsMessage.ShouldNotBeNull("Бот должен показать детали спота");
    spotDetailsMessage!.ShouldContainText("🐶 *Рекс*");
    spotDetailsMessage.ShouldContainText("Настроение");

    var detailsKeyboard = spotDetailsMessage.ShouldHaveInlineKeyboard();
    detailsKeyboard.ShouldContainButton("📋 Шаблоны задач");
    detailsKeyboard.ShouldContainButton("🗑️ Удалить спота");
    detailsKeyboard.ShouldContainButton("⬅️ Назад к списку");
  }

  [RetryFact(2)]
  public async Task TS_BOT_SPOT_003_DeleteSpot_ShouldConfirmAndRemoveSpot()
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    // Arrange: Admin creates family and spot
    var (familyName, adminTelegramId, adminChatId) =
      await BotFamilyFlowHelpers.CreateFamilyByGeolocationAsync(factory, "Семья Сидоровых");

    // Create a spot first
    var spotListMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      [UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "🧩 Споты")],
      adminChatId);
    var keyboard = spotListMessage!.ShouldHaveInlineKeyboard();
    var createButton = keyboard.GetButton("➕ Создать спота");

    var spotTypeMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      [UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, createButton.CallbackData!)],
      adminChatId);
    var spotTypeKeyboard = spotTypeMessage!.ShouldHaveInlineKeyboard();
    var catButton = spotTypeKeyboard.GetButton("🐱 Кот");

    await botClient.SendUpdateAndWaitForLastMessageAsync(
      [
        UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, catButton.CallbackData!),
        UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "Мурка")
      ],
      adminChatId);

    // Navigate to spot details
    botClient.Clear();
    var spotsMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "🧩 Споты"),
      adminChatId);
    var spotsKeyboard = spotsMessage!.ShouldHaveInlineKeyboard();
    var spotButton = spotsKeyboard.GetButton("🐱 Мурка");

    var spotDetailsMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, spotButton.CallbackData!),
      adminChatId);
    var detailsKeyboard = spotDetailsMessage!.ShouldHaveInlineKeyboard();
    var deleteButton = detailsKeyboard.GetButton("🗑️ Удалить спота");
    deleteButton.CallbackData.ShouldNotBeNull();

    // Act: Click delete button and wait for confirmation dialog
    var confirmationMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, deleteButton.CallbackData!),
      adminChatId);
    confirmationMessage.ShouldNotBeNull("Бот должен показать подтверждение удаления");
    confirmationMessage!.ShouldContainText("Удаление спота");
    confirmationMessage.ShouldContainText("Мурка");
    confirmationMessage.ShouldContainText("Внимание!");

    var confirmationKeyboard = confirmationMessage.ShouldHaveInlineKeyboard();
    var confirmButton = confirmationKeyboard.GetButton("✅ Да, удалить спота");
    confirmButton.CallbackData.ShouldNotBeNull();

    // Act: Confirm deletion and wait for success message
    var successMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, confirmButton.CallbackData!),
      adminChatId);
    successMessage.ShouldNotBeNull("Бот должен подтвердить удаление спота");
    successMessage!.ShouldContainText("✅ Спот успешно удалён");

    // Verify spot is removed from list
    botClient.Clear();
    var finalSpotListMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "🧩 Споты"),
      adminChatId);
    finalSpotListMessage.ShouldNotBeNull();
    finalSpotListMessage!.ShouldContainText("У вас пока нет спотов");
  }

  [RetryFact(2)]
  public async Task TS_BOT_SPOT_004_CancelDelete_ShouldReturnToSpotList()
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    // Arrange: Admin creates family and spot
    var (familyName, adminTelegramId, adminChatId) =
      await BotFamilyFlowHelpers.CreateFamilyByGeolocationAsync(factory, "Семья Тестовых");

    // Create a spot
    var spotListMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      [UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "🧩 Споты")],
      adminChatId);
    var keyboard = spotListMessage!.ShouldHaveInlineKeyboard();
    var createButton = keyboard.GetButton("➕ Создать спота");

    var spotTypeMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      [UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, createButton.CallbackData!)],
      adminChatId);
    var spotTypeKeyboard = spotTypeMessage!.ShouldHaveInlineKeyboard();
    var plantButton = spotTypeKeyboard.GetButton("🪴 Растение");

    await botClient.SendUpdateAndWaitForLastMessageAsync(
      [
        UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, plantButton.CallbackData!),
        UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "Фикус")
      ],
      adminChatId);

    // Navigate to spot details and click delete
    botClient.Clear();
    var spotsMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "🧩 Споты"),
      adminChatId);
    var spotsKeyboard = spotsMessage!.ShouldHaveInlineKeyboard();
    var spotButton = spotsKeyboard.GetButton("Фикус");

    var spotDetailsMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, spotButton.CallbackData!),
      adminChatId);
    var detailsKeyboard = spotDetailsMessage!.ShouldHaveInlineKeyboard();
    var deleteButton = detailsKeyboard.GetButton("🗑️ Удалить спота");

    var confirmationMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, deleteButton.CallbackData!),
      adminChatId);
    var confirmationKeyboard = confirmationMessage!.ShouldHaveInlineKeyboard();
    var cancelButton = confirmationKeyboard.GetButton("❌ Отмена");
    cancelButton.CallbackData.ShouldNotBeNull();

    // Act: Cancel deletion and wait for return to spot list
    var spotListAfterCancel = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, cancelButton.CallbackData!),
      adminChatId);
    spotListAfterCancel.ShouldNotBeNull("Бот должен вернуться к списку спотов");
    spotListAfterCancel!.ShouldContainText("Ваши споты");
    spotListAfterCancel.ShouldContainText("Фикус");
  }

  [RetryFact(2)]
  public async Task TS_BOT_SPOT_005_ManageResponsibles_ShouldToggleCheckboxOnMember()
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    // Arrange: Admin creates family and one spot
    var (_, adminTelegramId, adminChatId) =
      await BotFamilyFlowHelpers.CreateFamilyByGeolocationAsync(factory, "Семья Ответственных");

    // Create a spot
    var spotListMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      [UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "🧩 Споты")],
      adminChatId);
    var keyboard = spotListMessage!.ShouldHaveInlineKeyboard();
    var createButton = keyboard.GetButton("➕ Создать спота");

    var spotTypeMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      [UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, createButton.CallbackData!)],
      adminChatId);
    var spotTypeKeyboard = spotTypeMessage!.ShouldHaveInlineKeyboard();
    var catButton = spotTypeKeyboard.GetButton("🐱 Кот");

    var createMessages = await botClient.SendUpdateAndWaitForLastMessageAsync(
      [
        UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, catButton.CallbackData!),
        UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "Барсик")
      ],
      adminChatId);
    createMessages.ShouldNotBeNull();

    // Navigate to spot details
    botClient.Clear();
    var spotsMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "🧩 Споты"),
      adminChatId);
    var spotsKeyboard = spotsMessage!.ShouldHaveInlineKeyboard();
    var spotButton = spotsKeyboard.GetButton("🐱 Барсик");

    var spotDetailsMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, spotButton.CallbackData!),
      adminChatId);
    spotDetailsMessage.ShouldNotBeNull("Бот должен показать детали спота");
    spotDetailsMessage!.ShouldContainText("Барсик");

    var detailsKeyboard = spotDetailsMessage.ShouldHaveInlineKeyboard();
    var responsiblesButton = detailsKeyboard.GetButton("👥 Ответственные");
    responsiblesButton.CallbackData.ShouldNotBeNull();

    // Act: open responsibles screen
    var responsiblesMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, responsiblesButton.CallbackData!),
      adminChatId);

    // Assert: bot shows list of family members
    responsiblesMessage.ShouldNotBeNull("Бот должен показать экран управления ответственными");
    responsiblesMessage!.ShouldContainText("Ответственные за спота");
    var respKeyboard = responsiblesMessage.ShouldHaveInlineKeyboard();

    // Берём первого участника семьи из клавиатуры
    var firstMemberButton = respKeyboard.InlineKeyboard.First().First();
    firstMemberButton.CallbackData.ShouldNotBeNull();
    var memberName = firstMemberButton.Text.Replace("✅", string.Empty).Trim();

    // Изначально не должно быть галочки у имени
    firstMemberButton.Text.ShouldNotContain("✅");

    // Act: toggle responsibility for this member
    var afterToggleMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, firstMemberButton.CallbackData!),
      adminChatId);

    // Assert: в обновлённой клавиатуре у этого участника появилась галочка
    afterToggleMessage.ShouldNotBeNull("Бот должен обновить список ответственных");
    var afterToggleKeyboard = afterToggleMessage!.ShouldHaveInlineKeyboard();

    var updatedMemberButton = afterToggleKeyboard.InlineKeyboard
      .SelectMany(row => row)
      .First(btn => btn.Text.Contains(memberName));

    updatedMemberButton.Text.ShouldStartWith("✅");
  }

  [RetryFact(2)]
  public async Task TS_BOT_SPOT_006_Child_ShouldSeeResponsiblesAsTextWithoutToggleButtons()
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    // Arrange: admin creates family via bot flow
    var (_, adminTelegramId, adminChatId) =
      await BotFamilyFlowHelpers.CreateFamilyByGeolocationAsync(factory, "Семья Детская");

    // Admin opens family menu and creates invite with Child role
    var familyMenuMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "🏠 Семья"),
      adminChatId);
    familyMenuMessage.ShouldNotBeNull("Бот должен показать меню семьи");
    var familyMenuKeyboard = familyMenuMessage!.ShouldHaveInlineKeyboard();
    var createInviteButton = familyMenuKeyboard.GetButton("Создать приглашение");

    var inviteRoleMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, createInviteButton.CallbackData!),
      adminChatId);
    inviteRoleMessage.ShouldNotBeNull("Бот должен показать выбор роли для приглашения");
    var inviteRoleKeyboard = inviteRoleMessage!.ShouldHaveInlineKeyboard();
    var childRoleButton = inviteRoleKeyboard.GetButton("Ребёнок");

    var inviteMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, childRoleButton.CallbackData!),
      adminChatId);
    inviteMessage.ShouldNotBeNull("Бот должен отправить сообщение о создании приглашения");
    var inviteText = inviteMessage!.Text!;
    var match = Regex.Match(inviteText, @"invite_[A-Z0-9]+");
    match.Success.ShouldBeTrue("Пригласительная ссылка должна содержать payload вида invite_CODE");
    var invitePayload = match.Value;

    // Child joins family via /start invite_CODE
    var childTelegramId = TestDataBuilder.GenerateTelegramId();
    var childChatId = childTelegramId;

    botClient.Clear();

    await botClient.SendUpdateAndWaitForMessagesAsync(
      UpdateFactory.CreateTextUpdate(childChatId, childTelegramId, $"/start {invitePayload}", firstName: "Ребёнок"),
      childChatId,
      2);

    // Admin creates a spot via bot flow
    botClient.Clear();

    var spotListMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "🧩 Споты"),
      adminChatId);
    var keyboard = spotListMessage!.ShouldHaveInlineKeyboard();
    var createButton = keyboard.GetButton("➕ Создать спота");

    var spotTypeMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, createButton.CallbackData!),
      adminChatId);
    var spotTypeKeyboard = spotTypeMessage!.ShouldHaveInlineKeyboard();
    var catButton = spotTypeKeyboard.GetButton("🐱 Кот");

    await botClient.SendUpdateAndWaitForLastMessageAsync(
      [
        UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, catButton.CallbackData!),
        UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "Барсик")
      ],
      adminChatId);

    // Child opens spot details and responsibles screen
    botClient.Clear();

    var childSpotsMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(childChatId, childTelegramId, "🧩 Споты"),
      childChatId);
    childSpotsMessage.ShouldNotBeNull("Бот должен показать список спотов для ребёнка");
    childSpotsMessage!.ShouldContainText("Барсик");

    var childSpotsKeyboard = childSpotsMessage.ShouldHaveInlineKeyboard();
    var childSpotButton = childSpotsKeyboard.GetButton("Барсик");

    var childSpotDetails = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(childChatId, childTelegramId, childSpotButton.CallbackData!),
      childChatId);
    childSpotDetails.ShouldNotBeNull("Бот должен показать детали спота для ребёнка");

    var childDetailsKeyboard = childSpotDetails!.ShouldHaveInlineKeyboard();
    var childResponsiblesButton = childDetailsKeyboard.GetButton("👥 Ответственные");

    // Act: child opens responsibles screen
    var childResponsiblesMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(childChatId, childTelegramId, childResponsiblesButton.CallbackData!),
      childChatId);

    // Assert: ребёнок видит текстовый список, а не кнопки выбора участников
    childResponsiblesMessage.ShouldNotBeNull("Бот должен показать экран ответственных для ребёнка");
    childResponsiblesMessage!.ShouldContainText("Ответственные за спота");
    childResponsiblesMessage.ShouldContainText("Только взрослые участники семьи могут изменять ответственных");

    var childRespKeyboard = childResponsiblesMessage.ShouldHaveInlineKeyboard();

    // На клавиатуре должна быть только кнопка "Назад к споту"
    childRespKeyboard.InlineKeyboard.Count().ShouldBe(1);
    childRespKeyboard.InlineKeyboard.First().Count().ShouldBe(1);
    childRespKeyboard.InlineKeyboard.First().First().Text.ShouldContain("Назад к споту");
  }
}
