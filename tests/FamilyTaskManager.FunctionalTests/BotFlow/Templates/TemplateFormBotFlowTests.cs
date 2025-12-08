using FamilyTaskManager.FunctionalTests.Helpers;
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
    await NavigateToTemplatesAsync(botClient, adminChatId, adminTelegramId, "🐶 Барон");

    var templatesMessage = await botClient.WaitForLastMessageAsync(adminChatId);
    var createButton = templatesMessage!.ShouldHaveInlineKeyboard().GetButton("➕ Создать шаблон");

    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, createButton.CallbackData!));
    var titlePrompt = await botClient.WaitForLastMessageAsync(adminChatId);
    titlePrompt.ShouldNotBeNull();
    titlePrompt!.ShouldContainText("Введите название шаблона");

    botClient.EnqueueUpdate(UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "Выгулять собаку"));
    var pointsPrompt = await botClient.WaitForLastMessageAsync(adminChatId);
    pointsPrompt.ShouldNotBeNull();
    pointsPrompt!.ShouldContainText("Выберите сложность");

    var pointsKeyboard = pointsPrompt.ShouldHaveInlineKeyboard();
    var points3Button = pointsKeyboard.GetButton("⭐⭐⭐");

    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, points3Button.CallbackData!));
    var scheduleTypePrompt = await botClient.WaitForLastMessageAsync(adminChatId);
    scheduleTypePrompt.ShouldNotBeNull();
    scheduleTypePrompt!.ShouldContainText("Выберите тип расписания");

    var scheduleKeyboard = scheduleTypePrompt.ShouldHaveInlineKeyboard();
    var dailyButton = scheduleKeyboard.GetButton("Ежедневно");

    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, dailyButton.CallbackData!));
    var timePrompt = await botClient.WaitForLastMessageAsync(adminChatId);
    timePrompt.ShouldNotBeNull();
    timePrompt!.ShouldContainText("Введите время");

    botClient.EnqueueUpdate(UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "09:00"));
    var dueDurationPrompt = await botClient.WaitForLastMessageAsync(adminChatId);
    dueDurationPrompt.ShouldNotBeNull();
    dueDurationPrompt!.ShouldContainText("Введите срок выполнения");

    botClient.EnqueueUpdate(UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "12"));
    var successMessage = await botClient.WaitForLastMessageAsync(adminChatId);

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
    await NavigateToTemplatesAsync(botClient, adminChatId, adminTelegramId, "🐱 Мурзик");

    var templatesMessage = await botClient.WaitForLastMessageAsync(adminChatId);
    var createButton = templatesMessage!.ShouldHaveInlineKeyboard().GetButton("➕ Создать шаблон");

    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, createButton.CallbackData!));
    await botClient.WaitForLastMessageAsync(adminChatId);

    botClient.EnqueueUpdate(UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "Ветеринар"));
    var pointsPrompt = await botClient.WaitForLastMessageAsync(adminChatId);
    var points5Button = pointsPrompt!.ShouldHaveInlineKeyboard().GetButton("⭐⭐⭐⭐");

    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, points5Button.CallbackData!));
    var scheduleTypePrompt = await botClient.WaitForLastMessageAsync(adminChatId);
    var weeklyButton = scheduleTypePrompt!.ShouldHaveInlineKeyboard().GetButton("Еженедельно");

    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, weeklyButton.CallbackData!));
    await botClient.WaitForLastMessageAsync(adminChatId);

    botClient.EnqueueUpdate(UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "10:00"));
    var weekdayPrompt = await botClient.WaitForLastMessageAsync(adminChatId);
    weekdayPrompt.ShouldNotBeNull();
    weekdayPrompt!.ShouldContainText("Выберите день недели");

    var weekdayKeyboard = weekdayPrompt.ShouldHaveInlineKeyboard();
    var mondayButton = weekdayKeyboard.GetButton("Пн");

    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, mondayButton.CallbackData!));
    await botClient.WaitForLastMessageAsync(adminChatId);

    botClient.EnqueueUpdate(UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "12"));
    var successMessage = await botClient.WaitForLastMessageAsync(adminChatId);

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
    await NavigateToTemplatesAsync(botClient, adminChatId, adminTelegramId, "🪴 Фикус");

    var templatesMessage = await botClient.WaitForLastMessageAsync(adminChatId);
    var createButton = templatesMessage!.ShouldHaveInlineKeyboard().GetButton("➕ Создать шаблон");

    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, createButton.CallbackData!));
    await botClient.WaitForLastMessageAsync(adminChatId);

    botClient.EnqueueUpdate(UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "Пересадка"));
    var pointsPrompt = await botClient.WaitForLastMessageAsync(adminChatId);
    var points4Button = pointsPrompt!.ShouldHaveInlineKeyboard().GetButton("⭐⭐⭐⭐");

    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, points4Button.CallbackData!));
    var scheduleTypePrompt = await botClient.WaitForLastMessageAsync(adminChatId);
    var monthlyButton = scheduleTypePrompt!.ShouldHaveInlineKeyboard().GetButton("Ежемесячно");

    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, monthlyButton.CallbackData!));
    await botClient.WaitForLastMessageAsync(adminChatId);

    botClient.EnqueueUpdate(UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "15:00"));
    var monthDayPrompt = await botClient.WaitForLastMessageAsync(adminChatId);
    monthDayPrompt.ShouldNotBeNull();
    monthDayPrompt!.ShouldContainText("Введите день месяца");

    botClient.EnqueueUpdate(UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "15"));
    await botClient.WaitForLastMessageAsync(adminChatId);

    botClient.EnqueueUpdate(UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "12"));
    var successMessage = await botClient.WaitForLastMessageAsync(adminChatId);

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
    await NavigateToTemplatesAsync(botClient, adminChatId, adminTelegramId, "🐹 Пушок");

    var templatesMessage = await botClient.WaitForLastMessageAsync(adminChatId);
    var createButton = templatesMessage!.ShouldHaveInlineKeyboard().GetButton("➕ Создать шаблон");

    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, createButton.CallbackData!));
    await botClient.WaitForLastMessageAsync(adminChatId);

    botClient.EnqueueUpdate(UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "Почистить клетку"));
    var pointsPrompt = await botClient.WaitForLastMessageAsync(adminChatId);
    var points2Button = pointsPrompt!.ShouldHaveInlineKeyboard().GetButton("⭐⭐");

    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, points2Button.CallbackData!));
    var scheduleTypePrompt = await botClient.WaitForLastMessageAsync(adminChatId);
    var manualButton = scheduleTypePrompt!.ShouldHaveInlineKeyboard().GetButton("Вручную");

    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, manualButton.CallbackData!));
    var dueDurationPrompt = await botClient.WaitForLastMessageAsync(adminChatId);
    dueDurationPrompt.ShouldNotBeNull();
    dueDurationPrompt!.ShouldContainText("Введите срок выполнения");

    botClient.EnqueueUpdate(UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "6"));
    var successMessage = await botClient.WaitForLastMessageAsync(adminChatId);

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
    await NavigateToTemplatesAsync(botClient, adminChatId, adminTelegramId, "🐶 Рекс");

    var templatesMessage = await botClient.WaitForLastMessageAsync(adminChatId);
    var createButton = templatesMessage!.ShouldHaveInlineKeyboard().GetButton("➕ Создать шаблон");

    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, createButton.CallbackData!));
    await botClient.WaitForLastMessageAsync(adminChatId);

    botClient.EnqueueUpdate(UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "AB"));
    var errorMessage = await botClient.WaitForLastMessageAsync(adminChatId);

    errorMessage.ShouldNotBeNull();
    errorMessage!.ShouldContainText("Название шаблона должно содержать");
    errorMessage.ShouldContainText("символов");

    botClient.EnqueueUpdate(UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "Правильное название"));
    var pointsPrompt = await botClient.WaitForLastMessageAsync(adminChatId);
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
    await NavigateToTemplatesAsync(botClient, adminChatId, adminTelegramId, "🐱 Барсик");

    var templatesMessage = await botClient.WaitForLastMessageAsync(adminChatId);
    var createButton = templatesMessage!.ShouldHaveInlineKeyboard().GetButton("➕ Создать шаблон");

    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, createButton.CallbackData!));
    await botClient.WaitForLastMessageAsync(adminChatId);

    botClient.EnqueueUpdate(UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "Кормление"));
    var pointsPrompt = await botClient.WaitForLastMessageAsync(adminChatId);
    var points1Button = pointsPrompt!.ShouldHaveInlineKeyboard().GetButton("⭐");

    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, points1Button.CallbackData!));
    var scheduleTypePrompt = await botClient.WaitForLastMessageAsync(adminChatId);
    var dailyButton = scheduleTypePrompt!.ShouldHaveInlineKeyboard().GetButton("Ежедневно");

    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, dailyButton.CallbackData!));
    await botClient.WaitForLastMessageAsync(adminChatId);

    botClient.EnqueueUpdate(UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "25:00"));
    var errorMessage = await botClient.WaitForLastMessageAsync(adminChatId);

    errorMessage.ShouldNotBeNull();
    errorMessage!.ShouldContainText("Неверный формат времени");
    errorMessage.ShouldContainText("HH:mm");

    botClient.EnqueueUpdate(UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "08:30"));
    var dueDurationPrompt = await botClient.WaitForLastMessageAsync(adminChatId);
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
    await NavigateToTemplatesAsync(botClient, adminChatId, adminTelegramId, "🦜 Кеша");

    var templatesMessage = await botClient.WaitForLastMessageAsync(adminChatId);
    var createButton = templatesMessage!.ShouldHaveInlineKeyboard().GetButton("➕ Создать шаблон");

    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, createButton.CallbackData!));
    await botClient.WaitForLastMessageAsync(adminChatId);

    botClient.EnqueueUpdate(UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "Чистка клетки"));
    var pointsPrompt = await botClient.WaitForLastMessageAsync(adminChatId);
    var points2Button = pointsPrompt!.ShouldHaveInlineKeyboard().GetButton("⭐⭐");

    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, points2Button.CallbackData!));
    var scheduleTypePrompt = await botClient.WaitForLastMessageAsync(adminChatId);
    var manualButton = scheduleTypePrompt!.ShouldHaveInlineKeyboard().GetButton("Вручную");

    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, manualButton.CallbackData!));
    await botClient.WaitForLastMessageAsync(adminChatId);

    botClient.EnqueueUpdate(UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "25"));
    var errorMessage = await botClient.WaitForLastMessageAsync(adminChatId);

    errorMessage.ShouldNotBeNull();
    errorMessage!.ShouldContainText("Срок выполнения должен быть числом");
    errorMessage.ShouldContainText("0 до 24");

    botClient.EnqueueUpdate(UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "12"));
    var successMessage = await botClient.WaitForLastMessageAsync(adminChatId);
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

    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, editButton.CallbackData!));
    var editMenuMessage = await botClient.WaitForLastMessageAsync(adminChatId);
    editMenuMessage.ShouldNotBeNull();
    editMenuMessage!.ShouldContainText("Выберите поле для редактирования");

    var editKeyboard = editMenuMessage.ShouldHaveInlineKeyboard();
    var editTitleButton = editKeyboard.GetButton("Название");

    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, editTitleButton.CallbackData!));
    var titlePrompt = await botClient.WaitForLastMessageAsync(adminChatId);
    titlePrompt.ShouldNotBeNull();
    titlePrompt!.ShouldContainText("Введите новое название");

    botClient.EnqueueUpdate(UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "Новое название задачи"));
    var successMessage = await botClient.WaitForLastMessageAsync(adminChatId);

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

    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, editButton.CallbackData!));
    var editMenuMessage = await botClient.WaitForLastMessageAsync(adminChatId);

    var editKeyboard = editMenuMessage!.ShouldHaveInlineKeyboard();
    var editPointsButton = editKeyboard.GetButton("Очки");

    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, editPointsButton.CallbackData!));
    var pointsPrompt = await botClient.WaitForLastMessageAsync(adminChatId);
    pointsPrompt.ShouldNotBeNull();
    pointsPrompt!.ShouldContainText("Выберите новую сложность");

    var pointsKeyboard = pointsPrompt.ShouldHaveInlineKeyboard();
    var points5Button = pointsKeyboard.GetButton("⭐⭐⭐⭐");

    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, points5Button.CallbackData!));
    var updatedDetailsMessage = await botClient.WaitForLastMessageAsync(adminChatId);

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

    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, editButton.CallbackData!));
    var editMenuMessage = await botClient.WaitForLastMessageAsync(adminChatId);

    var editKeyboard = editMenuMessage!.ShouldHaveInlineKeyboard();
    var editDueDurationButton = editKeyboard.GetButton("Срок выполнения");

    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, editDueDurationButton.CallbackData!));
    var dueDurationPrompt = await botClient.WaitForLastMessageAsync(adminChatId);
    dueDurationPrompt.ShouldNotBeNull();
    dueDurationPrompt!.ShouldContainText("срок выполнения");

    botClient.EnqueueUpdate(UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "18"));
    var successMessage = await botClient.WaitForLastMessageAsync(adminChatId);

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

    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, editButton.CallbackData!));
    var editMenuMessage = await botClient.WaitForLastMessageAsync(adminChatId);

    var editKeyboard = editMenuMessage!.ShouldHaveInlineKeyboard();
    var editScheduleButton = editKeyboard.GetButton("Расписание");

    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, editScheduleButton.CallbackData!));
    var scheduleTypePrompt = await botClient.WaitForLastMessageAsync(adminChatId);
    scheduleTypePrompt.ShouldNotBeNull();
    scheduleTypePrompt!.ShouldContainText("Выберите тип расписания");

    var scheduleKeyboard = scheduleTypePrompt.ShouldHaveInlineKeyboard();
    var weeklyButton = scheduleKeyboard.GetButton("Еженедельно");

    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, weeklyButton.CallbackData!));
    var timePrompt = await botClient.WaitForLastMessageAsync(adminChatId);
    timePrompt.ShouldNotBeNull();
    timePrompt!.ShouldContainText("Введите время");

    botClient.EnqueueUpdate(UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "14:00"));
    var weekdayPrompt = await botClient.WaitForLastMessageAsync(adminChatId);
    weekdayPrompt.ShouldNotBeNull();
    weekdayPrompt!.ShouldContainText("Выберите день недели");

    var weekdayKeyboard = weekdayPrompt.ShouldHaveInlineKeyboard();
    var fridayButton = weekdayKeyboard.GetButton("Пт");

    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, fridayButton.CallbackData!));
    var dueDurationPrompt = await botClient.WaitForLastMessageAsync(adminChatId);
    dueDurationPrompt.ShouldNotBeNull();
    dueDurationPrompt!.ShouldContainText("Введите срок выполнения");

    botClient.EnqueueUpdate(UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "8"));
    var successMessage = await botClient.WaitForLastMessageAsync(adminChatId);

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

    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, editButton.CallbackData!));
    var editMenuMessage = await botClient.WaitForLastMessageAsync(adminChatId);

    var editKeyboard = editMenuMessage!.ShouldHaveInlineKeyboard();
    var editTitleButton = editKeyboard.GetButton("Название");

    botClient.EnqueueUpdate(
      UpdateFactory.CreateCallbackUpdate(adminChatId, adminTelegramId, editTitleButton.CallbackData!));
    await botClient.WaitForLastMessageAsync(adminChatId);

    botClient.EnqueueUpdate(UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "AB"));
    var errorMessage = await botClient.WaitForLastMessageAsync(adminChatId);

    errorMessage.ShouldNotBeNull();
    errorMessage!.ShouldContainText("Название шаблона должно содержать");

    botClient.EnqueueUpdate(UpdateFactory.CreateTextUpdate(adminChatId, adminTelegramId, "Правильное название"));
    var successMessage = await botClient.WaitForLastMessageAsync(adminChatId);
    successMessage.ShouldNotBeNull();
    successMessage!.ShouldContainText("успешно обновлён");
  }

  private async Task<Guid> CreateSpotAsync(dynamic botClient, long chatId, long telegramId,
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

    return Guid.NewGuid();
  }

  private async Task NavigateToTemplatesAsync(dynamic botClient, long chatId, long telegramId, string spotButtonText)
  {
    botClient.EnqueueUpdate(UpdateFactory.CreateTextUpdate(chatId, telegramId, "🧩 Споты"));
    Message spotsMessage = await botClient.WaitForLastMessageAsync(chatId);
    var spotButton = spotsMessage!.ShouldHaveInlineKeyboard().GetButton(spotButtonText);

    botClient.EnqueueUpdate(UpdateFactory.CreateCallbackUpdate(chatId, telegramId, spotButton.CallbackData!));
    Message spotDetailsMessage = await botClient.WaitForLastMessageAsync(chatId);
    var templatesButton = spotDetailsMessage!.ShouldHaveInlineKeyboard().GetButton("📋 Шаблоны задач");

    botClient.EnqueueUpdate(UpdateFactory.CreateCallbackUpdate(chatId, telegramId, templatesButton.CallbackData!));
  }

  private async Task<Message?> NavigateToFirstTemplateAsync(dynamic botClient, long chatId, long telegramId,
    string spotButtonText)
  {
    botClient.Clear();
    await NavigateToTemplatesAsync(botClient, chatId, telegramId, spotButtonText);

    Message templatesMessage = await botClient.WaitForLastMessageAsync(chatId);
    var firstTemplateButton = templatesMessage!.ShouldHaveInlineKeyboard().InlineKeyboard.First().First();

    botClient.EnqueueUpdate(UpdateFactory.CreateCallbackUpdate(chatId, telegramId, firstTemplateButton.CallbackData!));
    return await botClient.WaitForLastMessageAsync(chatId);
  }
}
