using FamilyTaskManager.Core.PetAggregate;
using FamilyTaskManager.Host.Modules.Bot.Helpers;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.Host.Modules.Bot.Services;
using FamilyTaskManager.UseCases.Pets;
using FamilyTaskManager.UseCases.Tasks;
using FamilyTaskManager.UseCases.TaskTemplates;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.ConversationHandlers;

public class TaskCreationHandler(
  ILogger<TaskCreationHandler> logger,
  IMediator mediator,
  IUserRegistrationService userRegistrationService)
  : BaseConversationHandler(logger, mediator)
{
  public async Task HandleTaskTitleInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string title,
    CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(title) || title.Length < 3 || title.Length > 100)
    {
      var keyboard = StateKeyboardHelper.GetKeyboardForState(ConversationState.AwaitingTaskTitle);
      await SendValidationErrorAsync(
        botClient,
        message.Chat.Id,
        "❌ Название задачи должно содержать от 3 до 100 символов. Попробуйте снова:",
        StateKeyboardHelper.GetHintForState(ConversationState.AwaitingTaskTitle),
        keyboard,
        cancellationToken);
      return;
    }

    // Store title and move to points input
    session.Data["title"] = title;
    session.State = ConversationState.AwaitingTaskPoints;

    var pointsKeyboard = StateKeyboardHelper.GetKeyboardForState(ConversationState.AwaitingTaskPoints);
    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      "💯 Введите количество очков за выполнение задачи (от 1 до 100):" +
      StateKeyboardHelper.GetHintForState(ConversationState.AwaitingTaskPoints),
      replyMarkup: pointsKeyboard,
      cancellationToken: cancellationToken);
  }

  public async Task HandleTaskPointsInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string pointsText,
    CancellationToken cancellationToken)
  {
    if (!int.TryParse(pointsText, out var points) || points < 1 || points > 100)
    {
      var keyboard = StateKeyboardHelper.GetKeyboardForState(ConversationState.AwaitingTaskPoints);
      await SendValidationErrorAsync(
        botClient,
        message.Chat.Id,
        "❌ Количество очков должно быть числом от 1 до 100. Попробуйте снова:",
        StateKeyboardHelper.GetHintForState(ConversationState.AwaitingTaskPoints),
        keyboard,
        cancellationToken);
      return;
    }

    // Store points and show pet selection
    session.Data["points"] = points;
    session.State = ConversationState.AwaitingTaskPetSelection;

    // Get family pets
    if (!TryGetSessionData<Guid>(session, "familyId", out var familyId))
    {
      await SendErrorAndClearStateAsync(
        botClient,
        message.Chat.Id,
        session,
        "❌ Ошибка. Попробуйте создать задачу заново.",
        cancellationToken);
      return;
    }

    var getPetsQuery = new GetPetsQuery(familyId);
    var petsResult = await Mediator.Send(getPetsQuery, cancellationToken);

    if (!petsResult.IsSuccess || !petsResult.Value.Any())
    {
      await SendErrorAndClearStateAsync(
        botClient,
        message.Chat.Id,
        session,
        BotConstants.Errors.NoPets,
        cancellationToken);
      return;
    }

    var buttons = petsResult.Value.Select(p =>
    {
      var petEmoji = p.Type switch
      {
        PetType.Cat => "🐱",
        PetType.Dog => "🐶",
        PetType.Hamster => "🐹",
        _ => "🐾"
      };
      return new[] { InlineKeyboardButton.WithCallbackData($"{petEmoji} {p.Name}", $"taskpet_{p.Id}") };
    }).ToArray();

    var petSelectionKeyboard = new InlineKeyboardMarkup(buttons);

    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      "🐾 Выберите питомца, к которому относится задача:",
      replyMarkup: petSelectionKeyboard,
      cancellationToken: cancellationToken);
  }

  public async Task HandleTaskDueDateInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string dueDateText,
    CancellationToken cancellationToken)
  {
    // Try to parse the date
    if (!int.TryParse(dueDateText, out var days) || days < 0 || days > 365)
    {
      var keyboard = StateKeyboardHelper.GetKeyboardForState(ConversationState.AwaitingTaskDueDate);
      await SendValidationErrorAsync(
        botClient,
        message.Chat.Id,
        "❌ Введите количество дней (от 0 до 365). Например: 1 (завтра), 7 (через неделю):",
        StateKeyboardHelper.GetHintForState(ConversationState.AwaitingTaskDueDate),
        keyboard,
        cancellationToken);
      return;
    }

    var dueAt = DateTime.UtcNow.AddDays(days);

    // Get all required data from session
    if (!TryGetSessionData<Guid>(session, "familyId", out var familyId) ||
        !TryGetSessionData<Guid>(session, "petId", out var petId) ||
        !TryGetSessionData<string>(session, "title", out var title) ||
        !TryGetSessionData<int>(session, "points", out var points))
    {
      await SendErrorAndClearStateAsync(
        botClient,
        message.Chat.Id,
        session,
        "❌ Ошибка. Попробуйте создать задачу заново.",
        cancellationToken);
      return;
    }

    // Get user ID
    var userResult = await userRegistrationService.GetOrRegisterUserAsync(message.From!, cancellationToken);

    if (!userResult.IsSuccess)
    {
      await SendErrorAndClearStateAsync(
        botClient,
        message.Chat.Id,
        session,
        "❌ Ошибка. Попробуйте /start",
        cancellationToken);
      return;
    }

    // Create one-time task
    var createTaskCommand = new CreateTaskCommand(familyId, petId, title, points, dueAt, userResult.Value);
    var result = await Mediator.Send(createTaskCommand, cancellationToken);

    if (!result.IsSuccess)
    {
      await SendErrorAndClearStateAsync(
        botClient,
        message.Chat.Id,
        session,
        $"❌ Ошибка создания задачи: {result.Errors.FirstOrDefault()}",
        cancellationToken);
      return;
    }

    session.ClearState();

    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      $"✅ Задача \"{title}\" успешно создана!\n\n" +
      $"💯 Очки: {points}\n" +
      $"📅 Срок: {dueAt:dd.MM.yyyy HH:mm}\n\n" +
      BotConstants.Messages.TaskAvailableToAll,
      replyMarkup: MainMenuHelper.GetMainMenuKeyboard(),
      cancellationToken: cancellationToken);
  }

  public async Task HandleTaskScheduleInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string schedule,
    CancellationToken cancellationToken)
  {
    // Validate schedule (basic check)
    if (string.IsNullOrWhiteSpace(schedule))
    {
      var keyboard = StateKeyboardHelper.GetKeyboardForState(ConversationState.AwaitingTaskSchedule);
      await SendValidationErrorAsync(
        botClient,
        message.Chat.Id,
        "❌ Расписание не может быть пустым. Попробуйте снова:",
        StateKeyboardHelper.GetHintForState(ConversationState.AwaitingTaskSchedule),
        keyboard,
        cancellationToken);
      return;
    }

    // Get all required data from session
    if (!TryGetSessionData<Guid>(session, "familyId", out var familyId) ||
        !TryGetSessionData<Guid>(session, "petId", out var petId) ||
        !TryGetSessionData<string>(session, "title", out var title) ||
        !TryGetSessionData<int>(session, "points", out var points))
    {
      await SendErrorAndClearStateAsync(
        botClient,
        message.Chat.Id,
        session,
        "❌ Ошибка. Попробуйте создать задачу заново.",
        cancellationToken);
      return;
    }

    // Get user ID
    var userResult = await userRegistrationService.GetOrRegisterUserAsync(message.From!, cancellationToken);

    if (!userResult.IsSuccess)
    {
      await SendErrorAndClearStateAsync(
        botClient,
        message.Chat.Id,
        session,
        "❌ Ошибка. Попробуйте /start",
        cancellationToken);
      return;
    }

    // Parse schedule
    var parseResult = ScheduleParser.Parse(schedule);
    if (!parseResult.IsSuccess)
    {
      var keyboard = StateKeyboardHelper.GetKeyboardForState(ConversationState.AwaitingTaskSchedule);
      await SendValidationErrorAsync(
        botClient,
        message.Chat.Id,
        $"❌ {parseResult.Errors.FirstOrDefault()}",
        StateKeyboardHelper.GetHintForState(ConversationState.AwaitingTaskSchedule),
        keyboard,
        cancellationToken);
      return;
    }

    var (scheduleType, scheduleTime, scheduleDayOfWeek, scheduleDayOfMonth) = parseResult.Value;

    // Create periodic task template
    var createTemplateCommand =
      new CreateTaskTemplateCommand(familyId, petId, title, points, scheduleType, scheduleTime, scheduleDayOfWeek,
        scheduleDayOfMonth, TimeSpan.FromHours(12), userResult.Value);
    var result = await Mediator.Send(createTemplateCommand, cancellationToken);

    if (!result.IsSuccess)
    {
      await SendErrorAndClearStateAsync(
        botClient,
        message.Chat.Id,
        session,
        $"❌ Ошибка создания задачи: {result.Errors.FirstOrDefault()}",
        cancellationToken);
      return;
    }

    session.ClearState();

    var scheduleText = ScheduleFormatter.Format(scheduleType, scheduleTime, scheduleDayOfWeek, scheduleDayOfMonth);
    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      $"✅ Периодическая задача \"{title}\" успешно создана!\n\n" +
      $"💯 Очки: {points}\n" +
      $"🔄 Расписание: {scheduleText}\n\n" +
      BotConstants.Messages.ScheduledTask,
      cancellationToken: cancellationToken);
  }
}
