using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.FunctionalTests.Helpers;
using FamilyTaskManager.Host;
using Telegram.Bot.Types;

namespace FamilyTaskManager.FunctionalTests.BotFlow.Templates;

public class TemplateBrowsingBotFlowTests(CustomWebApplicationFactory<Program> factory)
  : IClassFixture<CustomWebApplicationFactory<Program>>, IAsyncLifetime
{
  public Task InitializeAsync()
  {
    factory.CreateClient();
    return Task.CompletedTask;
  }

  public Task DisposeAsync() => Task.CompletedTask;

  [RetryFact(3)]
  public async Task TS_BOT_TEMPLATE_001_ViewSpotTemplates_ShouldShowTemplatesList()
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    var (familyName, adminTelegramId, adminChatId) =
      await BotFamilyFlowHelpers.CreateFamilyByGeolocationAsync(factory, "Семья Тестовых");

    await CreateSpotAsync(botClient, adminChatId, adminTelegramId, "🐶 Собака", "Рекс");

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

    templatesMessage.ShouldNotBeNull("Бот должен показать список шаблонов");
    templatesMessage!.ShouldContainText("Шаблоны задач для");
    templatesMessage.ShouldContainText("Рекс");
    templatesMessage.ShouldHaveInlineKeyboard();
  }

  [RetryFact(3)]
  public async Task TS_BOT_TEMPLATE_002_ViewTemplateDetails_ShouldShowTemplateInfo()
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    var (familyName, adminTelegramId, adminChatId) =
      await BotFamilyFlowHelpers.CreateFamilyByGeolocationAsync(factory, "Семья Петровых");

    await CreateSpotAsync(botClient, adminChatId, adminTelegramId, "🐱 Кот", "Мурзик");

    botClient.Clear();
    var spotsMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "🧩 Споты"),
      adminChatId);
    var spotButton = spotsMessage!.ShouldHaveInlineKeyboard().GetButton("🐱 Мурзик");

    var spotDetailsMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, spotButton.CallbackData!),
      adminChatId);
    var templatesButton = spotDetailsMessage!.ShouldHaveInlineKeyboard().GetButton("📋 Шаблоны задач");

    var templatesMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, templatesButton.CallbackData!),
      adminChatId);
    var templateKeyboard = templatesMessage!.ShouldHaveInlineKeyboard();
    var firstTemplateButton = templateKeyboard.InlineKeyboard.First().First();

    var templateDetailsMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, firstTemplateButton.CallbackData!),
      adminChatId);

    templateDetailsMessage.ShouldNotBeNull("Бот должен показать детали шаблона");
    templateDetailsMessage!.ShouldContainText("Шаблон задачи");
    templateDetailsMessage.ShouldContainText("Название:");
    templateDetailsMessage.ShouldContainText("Очки:");
    templateDetailsMessage.ShouldContainText("Расписание:");
    var detailsKeyboard = templateDetailsMessage.ShouldHaveInlineKeyboard();
    detailsKeyboard.ShouldContainButton("Создать задачу сейчас");
    detailsKeyboard.ShouldContainButton("Редактировать");
    detailsKeyboard.ShouldContainButton("Удалить");
  }

  [RetryFact(3)]
  public async Task TS_BOT_TEMPLATE_003_CreateTaskFromTemplate_ShouldCreateTaskSuccessfully()
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    var (familyName, adminTelegramId, adminChatId) =
      await BotFamilyFlowHelpers.CreateFamilyByGeolocationAsync(factory, "Семья Ивановых");

    await CreateSpotAsync(botClient, adminChatId, adminTelegramId, "🪴 Растение", "Фикус");

    botClient.Clear();
    var spotsMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "🧩 Споты"),
      adminChatId);
    var spotButton = spotsMessage!.ShouldHaveInlineKeyboard().GetButton("🪴 Фикус");

    var spotDetailsMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, spotButton.CallbackData!),
      adminChatId);
    var templatesButton = spotDetailsMessage!.ShouldHaveInlineKeyboard().GetButton("📋 Шаблоны задач");

    var templatesMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, templatesButton.CallbackData!),
      adminChatId);
    var firstTemplateButton = templatesMessage!.ShouldHaveInlineKeyboard().InlineKeyboard.First().First();

    var templateDetailsMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, firstTemplateButton.CallbackData!),
      adminChatId);
    var createTaskButton = templateDetailsMessage!.ShouldHaveInlineKeyboard().GetButton("Создать задачу сейчас");

    var taskCreatedMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, createTaskButton.CallbackData!),
      adminChatId);

    taskCreatedMessage.ShouldNotBeNull("Бот должен подтвердить создание задачи");
    taskCreatedMessage!.ShouldContainText("Задача создана");
    taskCreatedMessage.ShouldContainText("Название:");
    taskCreatedMessage.ShouldContainText("Спот:");
    taskCreatedMessage.ShouldContainText("Очки:");
    taskCreatedMessage.ShouldContainText("Срок выполнения:");
  }

  [RetryFact(3)]
  public async Task TS_BOT_TEMPLATE_004_DeleteTemplate_ShouldShowConfirmation()
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    var (familyName, adminTelegramId, adminChatId) =
      await BotFamilyFlowHelpers.CreateFamilyByGeolocationAsync(factory, "Семья Удаловых");

    await CreateSpotAsync(botClient, adminChatId, adminTelegramId, "🦜 Попугай", "Кеша");

    var templateDetailsMessage = await NavigateToFirstTemplateAsync(botClient, adminChatId, adminTelegramId, "🦜 Кеша");
    var deleteButton = templateDetailsMessage!.ShouldHaveInlineKeyboard().GetButton("Удалить");

    var confirmationMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, deleteButton.CallbackData!),
      adminChatId);

    confirmationMessage.ShouldNotBeNull("Бот должен показать подтверждение удаления");
    confirmationMessage!.ShouldContainText("Удаление шаблона");
    confirmationMessage.ShouldContainText("Вы уверены");
    var confirmKeyboard = confirmationMessage.ShouldHaveInlineKeyboard();
    confirmKeyboard.ShouldContainButton("Да, удалить");
    confirmKeyboard.ShouldContainButton("Отмена");
  }

  [RetryFact(3)]
  public async Task TS_BOT_TEMPLATE_005_ConfirmDeleteTemplate_ShouldDeleteSuccessfully()
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    var (familyName, adminTelegramId, adminChatId) =
      await BotFamilyFlowHelpers.CreateFamilyByGeolocationAsync(factory, "Семья Удаловых2");

    await CreateSpotAsync(botClient, adminChatId, adminTelegramId, "🐹 Хомяк", "Пушистик");

    var templateDetailsMessage =
      await NavigateToFirstTemplateAsync(botClient, adminChatId, adminTelegramId, "🐹 Пушистик");
    var deleteButton = templateDetailsMessage!.ShouldHaveInlineKeyboard().GetButton("Удалить");

    var confirmationMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, deleteButton.CallbackData!),
      adminChatId);
    var confirmButton = confirmationMessage!.ShouldHaveInlineKeyboard().GetButton("Да, удалить");

    var deletedMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, confirmButton.CallbackData!),
      adminChatId);

    deletedMessage.ShouldNotBeNull("Бот должен подтвердить удаление");
    deletedMessage!.ShouldContainText("Шаблон успешно удалён");
  }

  [RetryFact(3)]
  public async Task TS_BOT_TEMPLATE_006_CancelDeleteTemplate_ShouldReturnToTemplateDetails()
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    var (familyName, adminTelegramId, adminChatId) =
      await BotFamilyFlowHelpers.CreateFamilyByGeolocationAsync(factory, "Семья Отменовых");

    await CreateSpotAsync(botClient, adminChatId, adminTelegramId, "🐢 Черепаха", "Тортилла");

    var templateDetailsMessage =
      await NavigateToFirstTemplateAsync(botClient, adminChatId, adminTelegramId, "🐢 Тортилла");
    var deleteButton = templateDetailsMessage!.ShouldHaveInlineKeyboard().GetButton("Удалить");

    var confirmationMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, deleteButton.CallbackData!),
      adminChatId);
    var cancelButton = confirmationMessage!.ShouldHaveInlineKeyboard().GetButton("Отмена");

    var backToDetailsMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, cancelButton.CallbackData!),
      adminChatId);

    backToDetailsMessage.ShouldNotBeNull("Бот должен вернуться к деталям шаблона");
    backToDetailsMessage!.ShouldContainText("Шаблон задачи");
    backToDetailsMessage.ShouldContainText("Название:");
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

  private async Task<Message?> NavigateToFirstTemplateAsync(dynamic botClient, long chatId, long telegramId,
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

    return await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(chatId, telegramId, firstTemplateButton.CallbackData!),
      chatId);
  }


  [RetryFact(3)]
  public async Task TS_BOT_TEMPLATE_007_ViewResponsiblesList_ShouldShowFamilyMembers()
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    var (familyName, adminTelegramId, adminChatId) =
      await BotFamilyFlowHelpers.CreateFamilyByGeolocationAsync(factory, "Семья Ответственных");

    await CreateSpotAsync(botClient, adminChatId, adminTelegramId, "🐶 Собака", "Шарик");

    var templateDetailsMessage =
      await NavigateToFirstTemplateAsync(botClient, adminChatId, adminTelegramId, "🐶 Шарик");
    var responsiblesButton = templateDetailsMessage!.ShouldHaveInlineKeyboard().GetButton("👥 Ответственные");

    var responsiblesMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, responsiblesButton.CallbackData!),
      adminChatId);

    responsiblesMessage.ShouldNotBeNull("Бот должен показать список ответственных");
    responsiblesMessage!.ShouldContainText("Ответственные за шаблон задачи");
    responsiblesMessage.ShouldContainText("Нажмите на участника, чтобы назначить или снять ответственность");
    var keyboard = responsiblesMessage.ShouldHaveInlineKeyboard();
    keyboard.ShouldContainButton("Назад к шаблону");
  }

  [RetryFact(3)]
  public async Task TS_BOT_TEMPLATE_008_ToggleResponsible_ShouldAssignMember()
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    var (familyName, adminTelegramId, adminChatId) =
      await BotFamilyFlowHelpers.CreateFamilyByGeolocationAsync(factory, "Семья Назначений");

    var memberTelegramId = await BotFamilyFlowHelpers.AddFamilyMemberViaInviteAsync(
      botClient, adminChatId, adminTelegramId, FamilyRole.Adult, "Взрослый");

    await CreateSpotAsync(botClient, adminChatId, adminTelegramId, "🐱 Кот", "Мурзик");

    var templateDetailsMessage =
      await NavigateToFirstTemplateAsync(botClient, adminChatId, adminTelegramId, "🐱 Мурзик");
    var responsiblesButton = templateDetailsMessage!.ShouldHaveInlineKeyboard().GetButton("👥 Ответственные");

    var responsiblesMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, responsiblesButton.CallbackData!),
      adminChatId);

    var keyboard = responsiblesMessage!.ShouldHaveInlineKeyboard();
    var memberButton = keyboard.InlineKeyboard
      .SelectMany(row => row)
      .FirstOrDefault(btn => btn.Text.Contains("Взрослый"));

    memberButton.ShouldNotBeNull("Должна быть кнопка с участником семьи");

    var toggledMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, memberButton!.CallbackData!),
      adminChatId);

    toggledMessage.ShouldNotBeNull("Бот должен обновить список ответственных");
    toggledMessage!.ShouldContainText("Ответственные за шаблон задачи");
    var updatedKeyboard = toggledMessage.ShouldHaveInlineKeyboard();
    var updatedMemberButton = updatedKeyboard.InlineKeyboard
      .SelectMany(row => row)
      .FirstOrDefault(btn => btn.Text.Contains("Взрослый"));

    updatedMemberButton.ShouldNotBeNull("Кнопка участника должна остаться");
    Assert.Contains("✅", updatedMemberButton!.Text);
  }

  [RetryFact(3)]
  public async Task TS_BOT_TEMPLATE_009_ToggleResponsible_ShouldUnassignMember()
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    var (familyName, adminTelegramId, adminChatId) =
      await BotFamilyFlowHelpers.CreateFamilyByGeolocationAsync(factory, "Семья Снятий");

    var memberTelegramId = await BotFamilyFlowHelpers.AddFamilyMemberViaInviteAsync(
      botClient, adminChatId, adminTelegramId, FamilyRole.Adult, "Участник");

    await CreateSpotAsync(botClient, adminChatId, adminTelegramId, "🦜 Попугай", "Кеша");

    var templateDetailsMessage =
      await NavigateToFirstTemplateAsync(botClient, adminChatId, adminTelegramId, "🦜 Кеша");
    var responsiblesButton = templateDetailsMessage!.ShouldHaveInlineKeyboard().GetButton("👥 Ответственные");

    var responsiblesMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, responsiblesButton.CallbackData!),
      adminChatId);

    var keyboard = responsiblesMessage!.ShouldHaveInlineKeyboard();
    var memberButton = keyboard.InlineKeyboard
      .SelectMany(row => row)
      .FirstOrDefault(btn => btn.Text.Contains("Участник"));

    var assignedMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, memberButton!.CallbackData!),
      adminChatId);

    var assignedKeyboard = assignedMessage!.ShouldHaveInlineKeyboard();
    var assignedMemberButton = assignedKeyboard.InlineKeyboard
      .SelectMany(row => row)
      .FirstOrDefault(btn => btn.Text.Contains("Участник"));

    Assert.Contains("✅", assignedMemberButton!.Text);

    var unassignedMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, assignedMemberButton.CallbackData!),
      adminChatId);

    unassignedMessage.ShouldNotBeNull("Бот должен обновить список после снятия ответственности");
    var unassignedKeyboard = unassignedMessage!.ShouldHaveInlineKeyboard();
    var unassignedMemberButton = unassignedKeyboard.InlineKeyboard
      .SelectMany(row => row)
      .FirstOrDefault(btn => btn.Text.Contains("Участник"));

    unassignedMemberButton.ShouldNotBeNull("Кнопка участника должна остаться");
    Assert.DoesNotContain("✅", unassignedMemberButton!.Text);
  }

  [RetryFact(3)]
  public async Task TS_BOT_TEMPLATE_010_Child_ShouldSeeResponsiblesAsTextWithoutToggleButtons()
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    var (familyName, adminTelegramId, adminChatId) =
      await BotFamilyFlowHelpers.CreateFamilyByGeolocationAsync(factory, "Семья Детская Шаблонов");

    var childTelegramId = await BotFamilyFlowHelpers.AddFamilyMemberViaInviteAsync(
      botClient, adminChatId, adminTelegramId, FamilyRole.Child, "Ребёнок");

    botClient.Clear();
    await CreateSpotAsync(botClient, adminChatId, adminTelegramId, "🐱 Кот", "Барсик");

    var childTemplateDetails = await NavigateToFirstTemplateAsync(
      botClient, childTelegramId, childTelegramId, "🐱 Барсик");

    childTemplateDetails.ShouldNotBeNull("Бот должен показать детали шаблона для ребёнка");

    var childTemplateDetailsKeyboard = childTemplateDetails!.ShouldHaveInlineKeyboard();
    var childResponsiblesButton = childTemplateDetailsKeyboard.GetButton("👥 Ответственные");

    var childResponsiblesMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(childTelegramId, childTelegramId, childResponsiblesButton.CallbackData!),
      childTelegramId);

    childResponsiblesMessage.ShouldNotBeNull("Бот должен показать экран ответственных для ребёнка");
    childResponsiblesMessage!.ShouldContainText("Ответственные за шаблон задачи");
    childResponsiblesMessage.ShouldContainText("Только взрослые участники семьи могут изменять ответственных");

    var childRespKeyboard = childResponsiblesMessage.ShouldHaveInlineKeyboard();

    childRespKeyboard.InlineKeyboard.Count().ShouldBe(1);
    childRespKeyboard.InlineKeyboard.First().Count().ShouldBe(1);
    childRespKeyboard.InlineKeyboard.First().First().Text.ShouldContain("Назад к шаблону");
  }

  // Helper method to create spot and navigate to template (reusing existing code pattern)
  private async Task CreateSpotAndNavigateToTemplateAsync(dynamic botClient, long chatId, long telegramId,
    string spotType, string spotName)
  {
    // Create spot using existing method
    await CreateSpotAsync(botClient, chatId, telegramId, spotType, spotName);

    // Navigate to template using existing navigation pattern
    botClient.Clear();
    await NavigateToFirstTemplateAsync(botClient, chatId, telegramId, $"{spotName}");
  }

  // Helper to get last message (simple wrapper)
  private async Task<Message?> GetLastMessageAsync(dynamic botClient, long chatId)
  {
    // Wait a bit for any async operations
    await Task.Delay(100);
    var messages = botClient.GetMessages(chatId);
    return messages.LastOrDefault();
  }
}
