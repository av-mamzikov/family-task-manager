using FamilyTaskManager.Host.Modules.Bot.Helpers;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.UseCases.TaskTemplates;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.ConversationHandlers;

public class TemplateEditHandler(
  ILogger<TemplateEditHandler> logger,
  IMediator mediator) : BaseConversationHandler(logger, mediator)
{
  public async Task HandleTemplateEditTitleInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string title,
    CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(title) || title.Length < 3 || title.Length > 100)
    {
      var keyboard = StateKeyboardHelper.GetKeyboardForState(ConversationState.AwaitingTemplateEditTitle);
      await SendValidationErrorAsync(
        botClient,
        message.Chat.Id,
        "❌ Название шаблона должно содержать от 3 до 100 символов. Попробуйте снова:",
        StateKeyboardHelper.GetHintForState(ConversationState.AwaitingTemplateEditTitle),
        keyboard,
        cancellationToken);
      return;
    }

    if (!TryGetSessionData<Guid>(session, "templateId", out var templateId) ||
        !TryGetSessionData<Guid>(session, "familyId", out var familyId))
    {
      await SendErrorAndClearStateAsync(
        botClient,
        message.Chat.Id,
        session,
        "❌ Ошибка. Попробуйте снова.",
        cancellationToken);
      return;
    }

    var updateCommand = new UpdateTaskTemplateCommand(templateId, familyId, title, null, null, null);
    var result = await Mediator.Send(updateCommand, cancellationToken);

    if (!result.IsSuccess)
    {
      await SendErrorAndClearStateAsync(
        botClient,
        message.Chat.Id,
        session,
        $"❌ Ошибка обновления: {result.Errors.FirstOrDefault()}",
        cancellationToken);
      return;
    }

    session.ClearState();
    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      BotConstants.Templates.TemplateUpdated,
      replyMarkup: MainMenuHelper.GetMainMenuKeyboard(),
      cancellationToken: cancellationToken);
  }

  public async Task HandleTemplateEditPointsInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string pointsText,
    CancellationToken cancellationToken)
  {
    if (!int.TryParse(pointsText, out var points) || points < 1 || points > 100)
    {
      var keyboard = StateKeyboardHelper.GetKeyboardForState(ConversationState.AwaitingTemplateEditPoints);
      await SendValidationErrorAsync(
        botClient,
        message.Chat.Id,
        "❌ Количество очков должно быть числом от 1 до 100. Попробуйте снова:",
        StateKeyboardHelper.GetHintForState(ConversationState.AwaitingTemplateEditPoints),
        keyboard,
        cancellationToken);
      return;
    }

    if (!TryGetSessionData<Guid>(session, "templateId", out var templateId) ||
        !TryGetSessionData<Guid>(session, "familyId", out var familyId))
    {
      await SendErrorAndClearStateAsync(
        botClient,
        message.Chat.Id,
        session,
        "❌ Ошибка. Попробуйте снова.",
        cancellationToken);
      return;
    }

    var updateCommand = new UpdateTaskTemplateCommand(templateId, familyId, null, points, null, null);
    var result = await Mediator.Send(updateCommand, cancellationToken);

    if (!result.IsSuccess)
    {
      await SendErrorAndClearStateAsync(
        botClient,
        message.Chat.Id,
        session,
        $"❌ Ошибка обновления: {result.Errors.FirstOrDefault()}",
        cancellationToken);
      return;
    }

    session.ClearState();
    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      BotConstants.Templates.TemplateUpdated,
      replyMarkup: MainMenuHelper.GetMainMenuKeyboard(),
      cancellationToken: cancellationToken);
  }

  public async Task HandleTemplateEditScheduleInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string schedule,
    CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(schedule))
    {
      var keyboard = StateKeyboardHelper.GetKeyboardForState(ConversationState.AwaitingTemplateEditSchedule);
      await SendValidationErrorAsync(
        botClient,
        message.Chat.Id,
        "❌ Расписание не может быть пустым. Попробуйте снова:",
        StateKeyboardHelper.GetHintForState(ConversationState.AwaitingTemplateEditSchedule),
        keyboard,
        cancellationToken);
      return;
    }

    if (!TryGetSessionData<Guid>(session, "templateId", out var templateId) ||
        !TryGetSessionData<Guid>(session, "familyId", out var familyId))
    {
      await SendErrorAndClearStateAsync(
        botClient,
        message.Chat.Id,
        session,
        "❌ Ошибка. Попробуйте снова.",
        cancellationToken);
      return;
    }

    var updateCommand = new UpdateTaskTemplateCommand(templateId, familyId, null, null, schedule, null);
    var result = await Mediator.Send(updateCommand, cancellationToken);

    if (!result.IsSuccess)
    {
      await SendErrorAndClearStateAsync(
        botClient,
        message.Chat.Id,
        session,
        $"❌ Ошибка обновления: {result.Errors.FirstOrDefault()}\n\n" +
        BotConstants.Errors.InvalidCron,
        cancellationToken);
      return;
    }

    session.ClearState();
    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      BotConstants.Templates.TemplateUpdated,
      replyMarkup: MainMenuHelper.GetMainMenuKeyboard(),
      cancellationToken: cancellationToken);
  }

  public async Task HandleTemplateEditDueDurationInputAsync(
    ITelegramBotClient botClient,
    Message message,
    UserSession session,
    string dueDurationText,
    CancellationToken cancellationToken)
  {
    if (!int.TryParse(dueDurationText, out var dueDurationHours) || dueDurationHours < 0 || dueDurationHours > 8760)
    {
      var keyboard = StateKeyboardHelper.GetKeyboardForState(ConversationState.AwaitingTemplateEditDueDuration);
      await SendValidationErrorAsync(
        botClient,
        message.Chat.Id,
        "❌ Срок выполнения должен быть числом от 0 до 8760 часов. Попробуйте снова:",
        StateKeyboardHelper.GetHintForState(ConversationState.AwaitingTemplateEditDueDuration),
        keyboard,
        cancellationToken);
      return;
    }

    if (!TryGetSessionData<Guid>(session, "templateId", out var templateId) ||
        !TryGetSessionData<Guid>(session, "familyId", out var familyId))
    {
      await SendErrorAndClearStateAsync(
        botClient,
        message.Chat.Id,
        session,
        "❌ Ошибка. Попробуйте снова.",
        cancellationToken);
      return;
    }

    var dueDuration = TimeSpan.FromHours(dueDurationHours);
    var updateCommand = new UpdateTaskTemplateCommand(templateId, familyId, null, null, null, dueDuration);
    var result = await Mediator.Send(updateCommand, cancellationToken);

    if (!result.IsSuccess)
    {
      await SendErrorAndClearStateAsync(
        botClient,
        message.Chat.Id,
        session,
        $"❌ Ошибка обновления: {result.Errors.FirstOrDefault()}",
        cancellationToken);
      return;
    }

    session.ClearState();
    await botClient.SendTextMessageAsync(
      message.Chat.Id,
      BotConstants.Templates.TemplateUpdated,
      replyMarkup: MainMenuHelper.GetMainMenuKeyboard(),
      cancellationToken: cancellationToken);
  }
}
