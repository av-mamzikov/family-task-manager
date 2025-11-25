using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.Host.Modules.Bot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace FamilyTaskManager.Host.Modules.Bot.Handlers.CallbackHandlers;

public abstract class BaseCallbackHandler(
  ILogger logger,
  IMediator mediator,
  IUserRegistrationService userRegistrationService)
{
  protected readonly ILogger Logger = logger;
  protected readonly IMediator Mediator = mediator;
  protected readonly IUserRegistrationService UserRegistrationService = userRegistrationService;

  protected async Task<Guid?> GetOrRegisterUserAsync(User fromUser, CancellationToken cancellationToken)
  {
    var result = await UserRegistrationService.GetOrRegisterUserAsync(fromUser, cancellationToken);
    return result.IsSuccess ? result.Value : null;
  }

  protected async Task SendErrorAsync(
    ITelegramBotClient botClient,
    long chatId,
    string errorMessage,
    CancellationToken cancellationToken) =>
    await botClient.SendTextMessageAsync(
      chatId,
      errorMessage,
      cancellationToken: cancellationToken);

  protected async Task EditMessageWithErrorAsync(
    ITelegramBotClient botClient,
    long chatId,
    int messageId,
    string errorMessage,
    CancellationToken cancellationToken) =>
    await botClient.EditMessageTextAsync(
      chatId,
      messageId,
      errorMessage,
      cancellationToken: cancellationToken);

  protected bool TryGetSessionData<T>(
    UserSession session,
    string key,
    out T? value)
  {
    if (session.Data.TryGetValue(key, out var obj) && obj is T typedValue)
    {
      value = typedValue;
      return true;
    }

    value = default;
    return false;
  }
}
