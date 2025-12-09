using System.Text.Json;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.UseCases.Users;
using Quartz.Util;
using Telegram.Bot.Types;

namespace FamilyTaskManager.Host.Modules.Bot.Services;

public interface ISessionManager
{
  Task<UserSession> GetSessionAsync(User fromUser, CancellationToken cancellationToken = default);

  Task SaveSessionAsync(UserSession session, CancellationToken cancellationToken = default);
}

public class SessionManager(IMediator mediator, ILogger<SessionManager> logger) : ISessionManager
{
  public async Task<UserSession> GetSessionAsync(User fromUser,
    CancellationToken cancellationToken = default)
  {
    var telegramId = fromUser.Id;
    var username = string.Join(" ", new[] { fromUser.FirstName, fromUser.LastName });
    if (username.IsNullOrWhiteSpace())
      username = fromUser.Username ?? $"user{telegramId}";
    var result = await mediator.Send(new GetOrCreateTelegramSessionCommand(telegramId, username),
      cancellationToken);
    if (!result.IsSuccess)
      throw new Exception($"Failed to get session: {string.Join(",", result.Errors)}");
    var dbSession = result.Value;
    var session = new UserSession
    {
      TelegramId = telegramId,
      UserId = dbSession.UserId,
      CurrentFamilyId = dbSession.CurrentFamilyId,
      State = (ConversationState)dbSession.ConversationState,
      LastActivity = dbSession.LastActivity
    };

    if (!string.IsNullOrWhiteSpace(dbSession.SessionData))
      try
      {
        var data = JsonSerializer.Deserialize<UserSessionData>(dbSession.SessionData);
        if (data != null) session.Data = data;
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "Error deserializing session data");
      }

    return session;
  }

  public async Task SaveSessionAsync(UserSession session,
    CancellationToken cancellationToken = default)
  {
    var sessionDataJson = JsonSerializer.Serialize(session.Data);
    await mediator.Send(
      new SaveTelegramSessionCommand(session.TelegramId, session.CurrentFamilyId, (int)session.State, sessionDataJson),
      cancellationToken);
  }
}
