using FamilyTaskManager.Host.Modules.Bot.Helpers;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.Host.Modules.Bot.Services;
using FamilyTaskManager.UseCases.TaskTemplates;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.ConversationHandlers;

public class TemplateCreationHandler(
  ILogger<TemplateCreationHandler> logger,
  IMediator mediator,
  IUserRegistrationService userRegistrationService)
  : BaseConversationHandler(logger, mediator)
{
  public async Task HandleTemplateTitleInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string title,
    CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(title) || title.Length < 3 || title.Length > 100)
    {
      var keyboard = StateKeyboardHelper.GetKeyboardForState(ConversationState.AwaitingTemplateTitle);
      await SendValidationErrorAsync(
        botClient,
        message.Chat.Id,
        "❌ Название шаблона должно содержать от 3 до 100 символов. Попробуйте снова:",
        StateKeyboardHelper.GetHintForState(ConversationState.AwaitingTemplateTitle),
        keyboard,
        cancellationToken);
      return;
    }

    session.Data["title"] = title;
    session.State = ConversationState.AwaitingTemplatePoints;

    var pointsKeyboard = StateKeyboardHelper.GetKeyboardForState(ConversationState.AwaitingTemplatePoints);
    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      BotConstants.Templates.EnterTemplatePoints +
      StateKeyboardHelper.GetHintForState(ConversationState.AwaitingTemplatePoints),
      replyMarkup: pointsKeyboard,
      cancellationToken: cancellationToken);
  }

  public async Task HandleTemplatePointsInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string pointsText,
    CancellationToken cancellationToken)
  {
    if (!int.TryParse(pointsText, out var points) || points < 1 || points > 100)
    {
      var keyboard = StateKeyboardHelper.GetKeyboardForState(ConversationState.AwaitingTemplatePoints);
      await SendValidationErrorAsync(
        botClient,
        message.Chat.Id,
        "❌ Количество очков должно быть числом от 1 до 100. Попробуйте снова:",
        StateKeyboardHelper.GetHintForState(ConversationState.AwaitingTemplatePoints),
        keyboard,
        cancellationToken);
      return;
    }

    session.Data["points"] = points;
    session.State = ConversationState.AwaitingTemplateSchedule;

    var scheduleKeyboard = StateKeyboardHelper.GetKeyboardForState(ConversationState.AwaitingTemplateSchedule);
    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      BotConstants.Templates.EnterTemplateSchedule +
      StateKeyboardHelper.GetHintForState(ConversationState.AwaitingTemplateSchedule),
      parseMode: ParseMode.Markdown,
      replyMarkup: scheduleKeyboard,
      cancellationToken: cancellationToken);
  }

  public async Task HandleTemplateScheduleInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string schedule,
    CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(schedule))
    {
      var keyboard = StateKeyboardHelper.GetKeyboardForState(ConversationState.AwaitingTemplateSchedule);
      await SendValidationErrorAsync(
        botClient,
        message.Chat.Id,
        "❌ Расписание не может быть пустым. Попробуйте снова:",
        StateKeyboardHelper.GetHintForState(ConversationState.AwaitingTemplateSchedule),
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
        "❌ Ошибка. Попробуйте создать шаблон заново.",
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
      var keyboard = StateKeyboardHelper.GetKeyboardForState(ConversationState.AwaitingTemplateSchedule);
      await SendValidationErrorAsync(
        botClient,
        message.Chat.Id,
        $"❌ {parseResult.Errors.FirstOrDefault()}",
        StateKeyboardHelper.GetHintForState(ConversationState.AwaitingTemplateSchedule),
        keyboard,
        cancellationToken);
      return;
    }

    var (scheduleType, scheduleTime, scheduleDayOfWeek, scheduleDayOfMonth) = parseResult.Value;

    // Create template
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
        $"❌ Ошибка создания шаблона: {result.Errors.FirstOrDefault()}",
        cancellationToken);
      return;
    }

    session.ClearState();

    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      $"{BotConstants.Templates.TemplateCreated}\n\n" +
      $"✅ Шаблон \"{title}\" успешно создан!\n\n" +
      $"💯 Очки: {points}\n" +
      $"🔄 Расписание: {schedule}\n\n" +
      BotConstants.Messages.ScheduledTask,
      replyMarkup: MainMenuHelper.GetMainMenuKeyboard(),
      cancellationToken: cancellationToken);
  }
}
