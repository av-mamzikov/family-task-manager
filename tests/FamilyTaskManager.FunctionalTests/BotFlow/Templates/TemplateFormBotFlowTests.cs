using FamilyTaskManager.FunctionalTests.Helpers;
using FamilyTaskManager.TestInfrastructure;
using Telegram.Bot.Types;

namespace FamilyTaskManager.FunctionalTests.BotFlow.Templates;

public class TemplateFormBotFlowTests(CustomWebApplicationFactory<Program> factory)
  : IClassFixture<CustomWebApplicationFactory<Program>>, IAsyncLifetime
{
  public Task InitializeAsync()
  {
    factory.CreateClient();
    return Task.CompletedTask;
  }

  public Task DisposeAsync() => Task.CompletedTask;

  [Fact]
  public async Task TS_BOT_TEMPLATE_FORM_001_CreateDailyTemplate_ShouldCreateSuccessfully()
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    var (familyName, adminTelegramId, adminChatId) =
      await BotFamilyFlowHelpers.CreateFamilyByGeolocationAsync(factory, "Семья Ежедневных");

    var spotId = await CreateSpotAsync(botClient, adminChatId, adminTelegramId, "🐶 Собака", "Барон");

    botClient.Clear();
    var templatesMessage = await NavigateToTemplatesAsync(botClient, adminChatId, adminTelegramId, "🐶 Барон");

    var createButton = templatesMessage!.ShouldHaveInlineKeyboard().GetButton("➕ Создать шаблон");

    var titlePrompt = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, createButton.CallbackData!),
      adminChatId);
    titlePrompt.ShouldNotBeNull();
    titlePrompt!.ShouldContainText("Введите название шаблона");

    var pointsPrompt = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "Выгулять собаку"),
      adminChatId);
    pointsPrompt.ShouldNotBeNull();
    pointsPrompt!.ShouldContainText("Выберите сложность");

    var pointsKeyboard = pointsPrompt.ShouldHaveInlineKeyboard();
    var points3Button = pointsKeyboard.GetButton("⭐⭐⭐");

    var scheduleTypePrompt = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, points3Button.CallbackData!),
      adminChatId);
    scheduleTypePrompt.ShouldNotBeNull();
    scheduleTypePrompt!.ShouldContainText("Выберите тип расписания");

    var scheduleKeyboard = scheduleTypePrompt.ShouldHaveInlineKeyboard();
    var dailyButton = scheduleKeyboard.GetButton("Ежедневно");

    var timePrompt = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, dailyButton.CallbackData!),
      adminChatId);
    timePrompt.ShouldNotBeNull();
    timePrompt!.ShouldContainText("Введите время");

    var dueDurationPrompt = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "09:00"),
      adminChatId);
    dueDurationPrompt.ShouldNotBeNull();
    dueDurationPrompt!.ShouldContainText("Введите срок выполнения");

    var successMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "12"),
      adminChatId);

    successMessage.ShouldNotBeNull("Бот должен подтвердить создание шаблона");
    successMessage!.ShouldContainText("Шаблон");
    successMessage.ShouldContainText("Выгулять собаку");
    successMessage.ShouldContainText("успешно создан");
    successMessage.ShouldContainText("Очки:");
    successMessage.ShouldContainText("⭐⭐⭐");
    successMessage.ShouldContainText("Расписание:");
    successMessage.ShouldContainText("Ежедневно");
    successMessage.ShouldContainText("09:00");
  }

  [Fact]
  public async Task TS_BOT_TEMPLATE_FORM_002_CreateWeeklyTemplate_ShouldCreateSuccessfully()
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    var (familyName, adminTelegramId, adminChatId) =
      await BotFamilyFlowHelpers.CreateFamilyByGeolocationAsync(factory, "Семья Еженедельных");

    var spotId = await CreateSpotAsync(botClient, adminChatId, adminTelegramId, "🐱 Кот", "Мурзик");

    botClient.Clear();
    var templatesMessage = await NavigateToTemplatesAsync(botClient, adminChatId, adminTelegramId, "🐱 Мурзик");

    var createButton = templatesMessage!.ShouldHaveInlineKeyboard().GetButton("➕ Создать шаблон");

    await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, createButton.CallbackData!),
      adminChatId);

    var pointsPrompt = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "Ветеринар"),
      adminChatId);
    var points5Button = pointsPrompt!.ShouldHaveInlineKeyboard().GetButton("⭐⭐⭐⭐");

    var scheduleTypePrompt = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, points5Button.CallbackData!),
      adminChatId);
    var weeklyButton = scheduleTypePrompt!.ShouldHaveInlineKeyboard().GetButton("Еженедельно");

    await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, weeklyButton.CallbackData!),
      adminChatId);

    var weekdayPrompt = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "10:00"),
      adminChatId);
    weekdayPrompt.ShouldNotBeNull();
    weekdayPrompt!.ShouldContainText("Выберите день недели");

    var weekdayKeyboard = weekdayPrompt.ShouldHaveInlineKeyboard();
    var mondayButton = weekdayKeyboard.GetButton("Пн");

    await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, mondayButton.CallbackData!),
      adminChatId);

    var successMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "12"),
      adminChatId);

    successMessage.ShouldNotBeNull();
    successMessage!.ShouldContainText("Шаблон");
    successMessage.ShouldContainText("Ветеринар");
    successMessage.ShouldContainText("успешно создан");
    successMessage.ShouldContainText("⭐⭐⭐⭐");
    successMessage.ShouldContainText("Еженедельно");
    successMessage.ShouldContainText("Понедельник");
    successMessage.ShouldContainText("10:00");
  }

  [Fact]
  public async Task TS_BOT_TEMPLATE_FORM_003_CreateMonthlyTemplate_ShouldCreateSuccessfully()
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    var (familyName, adminTelegramId, adminChatId) =
      await BotFamilyFlowHelpers.CreateFamilyByGeolocationAsync(factory, "Семья Ежемесячных");

    var spotId = await CreateSpotAsync(botClient, adminChatId, adminTelegramId, "🪴 Растение", "Фикус");

    botClient.Clear();
    var templatesMessage = await NavigateToTemplatesAsync(botClient, adminChatId, adminTelegramId, "🪴 Фикус");
    var createButton = templatesMessage!.ShouldHaveInlineKeyboard().GetButton("➕ Создать шаблон");

    await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, createButton.CallbackData!),
      adminChatId);

    var pointsPrompt = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "Пересадка"),
      adminChatId);
    var points4Button = pointsPrompt!.ShouldHaveInlineKeyboard().GetButton("⭐⭐⭐⭐");

    var scheduleTypePrompt = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, points4Button.CallbackData!),
      adminChatId);
    var monthlyButton = scheduleTypePrompt!.ShouldHaveInlineKeyboard().GetButton("Ежемесячно");

    await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, monthlyButton.CallbackData!),
      adminChatId);

    var monthDayPrompt = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "15:00"),
      adminChatId);
    monthDayPrompt.ShouldNotBeNull();
    monthDayPrompt!.ShouldContainText("Введите день месяца");

    await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "15"),
      adminChatId);

    var successMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "12"),
      adminChatId);

    successMessage.ShouldNotBeNull();
    successMessage!.ShouldContainText("Шаблон");
    successMessage.ShouldContainText("Пересадка");
    successMessage.ShouldContainText("успешно создан");
    successMessage.ShouldContainText("⭐⭐⭐⭐");
    successMessage.ShouldContainText("Ежемесячно");
    successMessage.ShouldContainText("15-го числа");
    successMessage.ShouldContainText("15:00");
  }

  [Fact]
  public async Task TS_BOT_TEMPLATE_FORM_004_CreateManualTemplate_ShouldCreateSuccessfully()
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    var (familyName, adminTelegramId, adminChatId) =
      await BotFamilyFlowHelpers.CreateFamilyByGeolocationAsync(factory, "Семья Ручных");

    var spotId = await CreateSpotAsync(botClient, adminChatId, adminTelegramId, "🐹 Хомяк", "Пушок");

    botClient.Clear();
    var templatesMessage = await NavigateToTemplatesAsync(botClient, adminChatId, adminTelegramId, "🐹 Пушок");

    var createButton = templatesMessage!.ShouldHaveInlineKeyboard().GetButton("➕ Создать шаблон");

    await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, createButton.CallbackData!),
      adminChatId);

    var pointsPrompt = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "Почистить клетку"),
      adminChatId);
    var points2Button = pointsPrompt!.ShouldHaveInlineKeyboard().GetButton("⭐⭐");

    var scheduleTypePrompt = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, points2Button.CallbackData!),
      adminChatId);
    var manualButton = scheduleTypePrompt!.ShouldHaveInlineKeyboard().GetButton("Вручную");

    var dueDurationPrompt = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, manualButton.CallbackData!),
      adminChatId);
    dueDurationPrompt.ShouldNotBeNull();
    dueDurationPrompt!.ShouldContainText("Введите срок выполнения");

    var successMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "6"),
      adminChatId);

    successMessage.ShouldNotBeNull();
    successMessage!.ShouldContainText("Шаблон");
    successMessage.ShouldContainText("Почистить клетку");
    successMessage.ShouldContainText("успешно создан");
    successMessage.ShouldContainText("⭐⭐");
    successMessage.ShouldContainText("Вручную");
  }

  [Fact]
  public async Task TS_BOT_TEMPLATE_FORM_005_CreateTemplate_InvalidTitle_ShouldShowError()
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    var (familyName, adminTelegramId, adminChatId) =
      await BotFamilyFlowHelpers.CreateFamilyByGeolocationAsync(factory, "Семья Валидации");

    var spotId = await CreateSpotAsync(botClient, adminChatId, adminTelegramId, "🐶 Собака", "Рекс");

    botClient.Clear();
    var templatesMessage = await NavigateToTemplatesAsync(botClient, adminChatId, adminTelegramId, "🐶 Рекс");
    var createButton = templatesMessage!.ShouldHaveInlineKeyboard().GetButton("➕ Создать шаблон");

    await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, createButton.CallbackData!),
      adminChatId);

    var errorMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "AB"),
      adminChatId);

    errorMessage.ShouldNotBeNull();
    errorMessage!.ShouldContainText("Название шаблона должно содержать");
    errorMessage.ShouldContainText("символов");

    var pointsPrompt = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "Правильное название"),
      adminChatId);
    pointsPrompt.ShouldNotBeNull();
    pointsPrompt!.ShouldContainText("Выберите сложность");
  }

  [Fact]
  public async Task TS_BOT_TEMPLATE_FORM_006_CreateTemplate_InvalidTime_ShouldShowError()
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    var (familyName, adminTelegramId, adminChatId) =
      await BotFamilyFlowHelpers.CreateFamilyByGeolocationAsync(factory, "Семья Времени");

    var spotId = await CreateSpotAsync(botClient, adminChatId, adminTelegramId, "🐱 Кот", "Барсик");

    botClient.Clear();
    var templatesMessage = await NavigateToTemplatesAsync(botClient, adminChatId, adminTelegramId, "🐱 Барсик");

    var createButton = templatesMessage!.ShouldHaveInlineKeyboard().GetButton("➕ Создать шаблон");

    await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, createButton.CallbackData!),
      adminChatId);

    var pointsPrompt = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "Кормление"),
      adminChatId);
    var points1Button = pointsPrompt!.ShouldHaveInlineKeyboard().GetButton("⭐");

    var scheduleTypePrompt = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, points1Button.CallbackData!),
      adminChatId);
    var dailyButton = scheduleTypePrompt!.ShouldHaveInlineKeyboard().GetButton("Ежедневно");

    await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, dailyButton.CallbackData!),
      adminChatId);

    var errorMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "25:00"),
      adminChatId);

    errorMessage.ShouldNotBeNull();
    errorMessage!.ShouldContainText("Неверный формат времени");
    errorMessage.ShouldContainText("HH:mm");

    var dueDurationPrompt = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "08:30"),
      adminChatId);
    dueDurationPrompt.ShouldNotBeNull();
    dueDurationPrompt!.ShouldContainText("Введите срок выполнения");
  }

  [Fact]
  public async Task TS_BOT_TEMPLATE_FORM_007_CreateTemplate_InvalidDueDuration_ShouldShowError()
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    var (familyName, adminTelegramId, adminChatId) =
      await BotFamilyFlowHelpers.CreateFamilyByGeolocationAsync(factory, "Семья Сроков");

    var spotId = await CreateSpotAsync(botClient, adminChatId, adminTelegramId, "🦜 Попугай", "Кеша");

    botClient.Clear();
    var templatesMessage = await NavigateToTemplatesAsync(botClient, adminChatId, adminTelegramId, "🦜 Кеша");

    var createButton = templatesMessage!.ShouldHaveInlineKeyboard().GetButton("➕ Создать шаблон");

    await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, createButton.CallbackData!),
      adminChatId);

    var pointsPrompt = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "Чистка клетки"),
      adminChatId);
    var points2Button = pointsPrompt!.ShouldHaveInlineKeyboard().GetButton("⭐⭐");

    var scheduleTypePrompt = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, points2Button.CallbackData!),
      adminChatId);
    var manualButton = scheduleTypePrompt!.ShouldHaveInlineKeyboard().GetButton("Вручную");

    await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, manualButton.CallbackData!),
      adminChatId);

    var errorMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "25"),
      adminChatId);

    errorMessage.ShouldNotBeNull();
    errorMessage!.ShouldContainText("Срок выполнения должен быть числом");
    errorMessage.ShouldContainText("0 до 24");

    var successMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "12"),
      adminChatId);
    successMessage.ShouldNotBeNull();
    successMessage!.ShouldContainText("успешно создан");
  }

  [Fact]
  public async Task TS_BOT_TEMPLATE_FORM_008_EditTemplateTitle_ShouldUpdateSuccessfully()
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    var (familyName, adminTelegramId, adminChatId) =
      await BotFamilyFlowHelpers.CreateFamilyByGeolocationAsync(factory, "Семья Редактирования");

    var spotId = await CreateSpotAsync(botClient, adminChatId, adminTelegramId, "🐶 Собака", "Шарик");

    botClient.Clear();
    var templateDetailsMessage =
      await NavigateToFirstTemplateAsync(botClient, adminChatId, adminTelegramId, "🐶 Шарик");
    var editButton = templateDetailsMessage!.ShouldHaveInlineKeyboard().GetButton("Редактировать");

    var editMenuMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, editButton.CallbackData!),
      adminChatId);
    editMenuMessage.ShouldNotBeNull();
    editMenuMessage!.ShouldContainText("Выберите поле для редактирования");

    var editKeyboard = editMenuMessage.ShouldHaveInlineKeyboard();
    var editTitleButton = editKeyboard.GetButton("Название");

    var titlePrompt = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, editTitleButton.CallbackData!),
      adminChatId);
    titlePrompt.ShouldNotBeNull();
    titlePrompt!.ShouldContainText("Введите новое название");

    var successMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "Новое название задачи"),
      adminChatId);

    successMessage.ShouldNotBeNull();
    successMessage!.ShouldContainText("успешно обновлён");
  }

  [Fact]
  public async Task TS_BOT_TEMPLATE_FORM_009_EditTemplatePoints_ShouldUpdateSuccessfully()
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    var (familyName, adminTelegramId, adminChatId) =
      await BotFamilyFlowHelpers.CreateFamilyByGeolocationAsync(factory, "Семья Очков");

    var spotId = await CreateSpotAsync(botClient, adminChatId, adminTelegramId, "🐱 Кот", "Васька");

    botClient.Clear();
    var templateDetailsMessage =
      await NavigateToFirstTemplateAsync(botClient, adminChatId, adminTelegramId, "🐱 Васька");
    var editButton = templateDetailsMessage!.ShouldHaveInlineKeyboard().GetButton("Редактировать");

    var editMenuMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, editButton.CallbackData!),
      adminChatId);

    var editKeyboard = editMenuMessage!.ShouldHaveInlineKeyboard();
    var editPointsButton = editKeyboard.GetButton("Очки");

    var pointsPrompt = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, editPointsButton.CallbackData!),
      adminChatId);
    pointsPrompt.ShouldNotBeNull();
    pointsPrompt!.ShouldContainText("Выберите новую сложность");

    var pointsKeyboard = pointsPrompt.ShouldHaveInlineKeyboard();
    var points5Button = pointsKeyboard.GetButton("⭐⭐⭐⭐");

    var updatedDetailsMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, points5Button.CallbackData!),
      adminChatId);

    updatedDetailsMessage.ShouldNotBeNull();
    updatedDetailsMessage!.ShouldContainText("Редактирование шаблона");
  }

  [Fact]
  public async Task TS_BOT_TEMPLATE_FORM_010_EditTemplateDueDuration_ShouldUpdateSuccessfully()
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    var (familyName, adminTelegramId, adminChatId) =
      await BotFamilyFlowHelpers.CreateFamilyByGeolocationAsync(factory, "Семья Дедлайнов");

    var spotId = await CreateSpotAsync(botClient, adminChatId, adminTelegramId, "🪴 Растение", "Алоэ");

    botClient.Clear();
    var templateDetailsMessage =
      await NavigateToFirstTemplateAsync(botClient, adminChatId, adminTelegramId, "🪴 Алоэ");
    var editButton = templateDetailsMessage!.ShouldHaveInlineKeyboard().GetButton("Редактировать");

    var editMenuMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, editButton.CallbackData!),
      adminChatId);

    var editKeyboard = editMenuMessage!.ShouldHaveInlineKeyboard();
    var editDueDurationButton = editKeyboard.GetButton("Срок выполнения");

    var dueDurationPrompt = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, editDueDurationButton.CallbackData!),
      adminChatId);
    dueDurationPrompt.ShouldNotBeNull();
    dueDurationPrompt!.ShouldContainText("срок выполнения");

    var successMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "18"),
      adminChatId);

    successMessage.ShouldNotBeNull();
    successMessage!.ShouldContainText("успешно обновлён");
  }

  [Fact]
  public async Task TS_BOT_TEMPLATE_FORM_011_EditTemplateSchedule_ShouldUpdateSuccessfully()
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    var (familyName, adminTelegramId, adminChatId) =
      await BotFamilyFlowHelpers.CreateFamilyByGeolocationAsync(factory, "Семья Расписаний");

    var spotId = await CreateSpotAsync(botClient, adminChatId, adminTelegramId, "🐹 Хомяк", "Хома");

    botClient.Clear();
    var templateDetailsMessage =
      await NavigateToFirstTemplateAsync(botClient, adminChatId, adminTelegramId, "🐹 Хома");
    var editButton = templateDetailsMessage!.ShouldHaveInlineKeyboard().GetButton("Редактировать");

    var editMenuMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, editButton.CallbackData!),
      adminChatId);

    var editKeyboard = editMenuMessage!.ShouldHaveInlineKeyboard();
    var editScheduleButton = editKeyboard.GetButton("Расписание");

    var scheduleTypePrompt = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, editScheduleButton.CallbackData!),
      adminChatId);
    scheduleTypePrompt.ShouldNotBeNull();
    scheduleTypePrompt!.ShouldContainText("Выберите тип расписания");

    var scheduleKeyboard = scheduleTypePrompt.ShouldHaveInlineKeyboard();
    var weeklyButton = scheduleKeyboard.GetButton("Еженедельно");

    var timePrompt = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, weeklyButton.CallbackData!),
      adminChatId);
    timePrompt.ShouldNotBeNull();
    timePrompt!.ShouldContainText("Введите время");

    var weekdayPrompt = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "14:00"),
      adminChatId);
    weekdayPrompt.ShouldNotBeNull();
    weekdayPrompt!.ShouldContainText("Выберите день недели");

    var weekdayKeyboard = weekdayPrompt.ShouldHaveInlineKeyboard();
    var fridayButton = weekdayKeyboard.GetButton("Пт");

    var dueDurationPrompt = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, fridayButton.CallbackData!),
      adminChatId);
    dueDurationPrompt.ShouldNotBeNull();
    dueDurationPrompt!.ShouldContainText("Введите срок выполнения");

    var successMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "8"),
      adminChatId);

    successMessage.ShouldNotBeNull();
    successMessage!.ShouldContainText("успешно обновлён");
  }

  [Fact]
  public async Task TS_BOT_TEMPLATE_FORM_012_EditTemplate_InvalidTitle_ShouldShowError()
  {
    var botClient = factory.TelegramBotClient;
    botClient.Clear();

    var (familyName, adminTelegramId, adminChatId) =
      await BotFamilyFlowHelpers.CreateFamilyByGeolocationAsync(factory, "Семья Ошибок");

    var spotId = await CreateSpotAsync(botClient, adminChatId, adminTelegramId, "🐢 Черепаха", "Тортилла");

    botClient.Clear();
    var templateDetailsMessage =
      await NavigateToFirstTemplateAsync(botClient, adminChatId, adminTelegramId, "🐢 Тортилла");
    var editButton = templateDetailsMessage!.ShouldHaveInlineKeyboard().GetButton("Редактировать");

    var editMenuMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, editButton.CallbackData!),
      adminChatId);

    var editKeyboard = editMenuMessage!.ShouldHaveInlineKeyboard();
    var editTitleButton = editKeyboard.GetButton("Название");

    await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, editTitleButton.CallbackData!),
      adminChatId);

    var errorMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "AB"),
      adminChatId);

    errorMessage.ShouldNotBeNull();
    errorMessage!.ShouldContainText("Название шаблона должно содержать");

    var successMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "Правильное название"),
      adminChatId);
    successMessage.ShouldNotBeNull();
    successMessage!.ShouldContainText("успешно обновлён");
  }

  private async Task<Guid> CreateSpotAsync(dynamic botClient, long chatId, long telegramId,
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

    return Guid.NewGuid();
  }

  private async Task<Message?> NavigateToTemplatesAsync(dynamic botClient, long chatId, long telegramId,
    string spotButtonText)
  {
    Message spotsMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateTextUpdate(chatId, telegramId, "🧩 Споты"),
      chatId);
    var spotButton = spotsMessage!.ShouldHaveInlineKeyboard().GetButton(spotButtonText);

    Message spotDetailsMessage = await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(chatId, telegramId, spotButton.CallbackData!),
      chatId);
    var templatesButton = spotDetailsMessage!.ShouldHaveInlineKeyboard().GetButton("📋 Шаблоны задач");

    return await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(chatId, telegramId, templatesButton.CallbackData!), chatId);
  }

  private async Task<Message?> NavigateToFirstTemplateAsync(TestTelegramBotClient botClient, long chatId,
    long telegramId,
    string spotButtonText)
  {
    botClient.Clear();
    var templatesMessage = await NavigateToTemplatesAsync(botClient, chatId, telegramId, spotButtonText);

    var firstTemplateButton = templatesMessage!.ShouldHaveInlineKeyboard().InlineKeyboard.First().First();

    return await botClient.SendUpdateAndWaitForLastMessageAsync(
      UpdateFactory.CreateCallbackUpdate(chatId, telegramId, firstTemplateButton.CallbackData!),
      chatId);
  }
}
