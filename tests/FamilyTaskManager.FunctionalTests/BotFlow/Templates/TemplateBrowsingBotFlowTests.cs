using FamilyTaskManager.FunctionalTests.Helpers;
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

  [Fact]
  public async Task TS_BOT_TEMPLATE_001_ViewSpotTemplates_ShouldShowTemplatesList()
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    var (familyName, adminTelegramId, adminChatId) =
      await BotFamilyFlowHelpers.CreateFamilyByGeolocationAsync(factory, "Семья Тестовых");

    await CreateSpotAsync(botClient, adminChatId, adminTelegramId, "🐶 Собака", "Рекс");

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

    templatesMessage.ShouldNotBeNull("Бот должен показать список шаблонов");
    templatesMessage!.ShouldContainText("Шаблоны задач для");
    templatesMessage.ShouldContainText("Рекс");
    templatesMessage.ShouldHaveInlineKeyboard();
  }

  [Fact]
  public async Task TS_BOT_TEMPLATE_002_ViewTemplateDetails_ShouldShowTemplateInfo()
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    var (familyName, adminTelegramId, adminChatId) =
      await BotFamilyFlowHelpers.CreateFamilyByGeolocationAsync(factory, "Семья Петровых");

    await CreateSpotAsync(botClient, adminChatId, adminTelegramId, "🐱 Кот", "Мурзик");

    botClient.Clear();
    botClient.EnqueueUpdate(UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "🧩 Споты"));
    var spotsMessage = await botClient.WaitForLastMessageAsync(adminChatId);
    var spotButton = spotsMessage!.ShouldHaveInlineKeyboard().GetButton("🐱 Мурзик");

    botClient.EnqueueUpdate(UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, spotButton.CallbackData!));
    var spotDetailsMessage = await botClient.WaitForLastMessageAsync(adminChatId);
    var templatesButton = spotDetailsMessage!.ShouldHaveInlineKeyboard().GetButton("📋 Шаблоны задач");

    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, templatesButton.CallbackData!));
    var templatesMessage = await botClient.WaitForLastMessageAsync(adminChatId);
    var templateKeyboard = templatesMessage!.ShouldHaveInlineKeyboard();
    var firstTemplateButton = templateKeyboard.InlineKeyboard.First().First();

    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, firstTemplateButton.CallbackData!));
    var templateDetailsMessage = await botClient.WaitForLastMessageAsync(adminChatId);

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

  [Fact]
  public async Task TS_BOT_TEMPLATE_003_CreateTaskFromTemplate_ShouldCreateTaskSuccessfully()
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    var (familyName, adminTelegramId, adminChatId) =
      await BotFamilyFlowHelpers.CreateFamilyByGeolocationAsync(factory, "Семья Ивановых");

    await CreateSpotAsync(botClient, adminChatId, adminTelegramId, "🪴 Растение", "Фикус");

    botClient.Clear();
    botClient.EnqueueUpdate(UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "🧩 Споты"));
    var spotsMessage = await botClient.WaitForLastMessageAsync(adminChatId);
    var spotButton = spotsMessage!.ShouldHaveInlineKeyboard().GetButton("🪴 Фикус");

    botClient.EnqueueUpdate(UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, spotButton.CallbackData!));
    var spotDetailsMessage = await botClient.WaitForLastMessageAsync(adminChatId);
    var templatesButton = spotDetailsMessage!.ShouldHaveInlineKeyboard().GetButton("📋 Шаблоны задач");

    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, templatesButton.CallbackData!));
    var templatesMessage = await botClient.WaitForLastMessageAsync(adminChatId);
    var firstTemplateButton = templatesMessage!.ShouldHaveInlineKeyboard().InlineKeyboard.First().First();

    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, firstTemplateButton.CallbackData!));
    var templateDetailsMessage = await botClient.WaitForLastMessageAsync(adminChatId);
    var createTaskButton = templateDetailsMessage!.ShouldHaveInlineKeyboard().GetButton("Создать задачу сейчас");

    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, createTaskButton.CallbackData!));
    var taskCreatedMessage = await botClient.WaitForLastMessageAsync(adminChatId);

    taskCreatedMessage.ShouldNotBeNull("Бот должен подтвердить создание задачи");
    taskCreatedMessage!.ShouldContainText("Задача создана");
    taskCreatedMessage.ShouldContainText("Название:");
    taskCreatedMessage.ShouldContainText("Спот:");
    taskCreatedMessage.ShouldContainText("Очки:");
    taskCreatedMessage.ShouldContainText("Срок выполнения:");
  }

  [Fact]
  public async Task TS_BOT_TEMPLATE_004_DeleteTemplate_ShouldShowConfirmation()
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    var (familyName, adminTelegramId, adminChatId) =
      await BotFamilyFlowHelpers.CreateFamilyByGeolocationAsync(factory, "Семья Удаловых");

    await CreateSpotAsync(botClient, adminChatId, adminTelegramId, "🦜 Попугай", "Кеша");

    var templateDetailsMessage = await NavigateToFirstTemplateAsync(botClient, adminChatId, adminTelegramId, "🦜 Кеша");
    var deleteButton = templateDetailsMessage!.ShouldHaveInlineKeyboard().GetButton("Удалить");

    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, deleteButton.CallbackData!));
    var confirmationMessage = await botClient.WaitForLastMessageAsync(adminChatId);

    confirmationMessage.ShouldNotBeNull("Бот должен показать подтверждение удаления");
    confirmationMessage!.ShouldContainText("Удаление шаблона");
    confirmationMessage.ShouldContainText("Вы уверены");
    var confirmKeyboard = confirmationMessage.ShouldHaveInlineKeyboard();
    confirmKeyboard.ShouldContainButton("Да, удалить");
    confirmKeyboard.ShouldContainButton("Отмена");
  }

  [Fact]
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

    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, deleteButton.CallbackData!));
    var confirmationMessage = await botClient.WaitForLastMessageAsync(adminChatId);
    var confirmButton = confirmationMessage!.ShouldHaveInlineKeyboard().GetButton("Да, удалить");

    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, confirmButton.CallbackData!));
    var deletedMessage = await botClient.WaitForLastMessageAsync(adminChatId);

    deletedMessage.ShouldNotBeNull("Бот должен подтвердить удаление");
    deletedMessage!.ShouldContainText("Шаблон успешно удалён");
  }

  [Fact]
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

    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, deleteButton.CallbackData!));
    var confirmationMessage = await botClient.WaitForLastMessageAsync(adminChatId);
    var cancelButton = confirmationMessage!.ShouldHaveInlineKeyboard().GetButton("Отмена");

    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, cancelButton.CallbackData!));
    var backToDetailsMessage = await botClient.WaitForLastMessageAsync(adminChatId);

    backToDetailsMessage.ShouldNotBeNull("Бот должен вернуться к деталям шаблона");
    backToDetailsMessage!.ShouldContainText("Шаблон задачи");
    backToDetailsMessage.ShouldContainText("Название:");
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

  private async Task<Message?> NavigateToFirstTemplateAsync(dynamic botClient, long chatId, long telegramId,
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
    return await botClient.WaitForLastMessageAsync(chatId);
  }
}
