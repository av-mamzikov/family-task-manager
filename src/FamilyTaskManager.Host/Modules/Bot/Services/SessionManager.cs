using System.Collections.Concurrent;
using System.Text.Json;
using FamilyTaskManager.Core.Specifications;
using FamilyTaskManager.Core.UserAggregate;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.UseCases.Users.Specifications;

namespace FamilyTaskManager.Host.Modules.Bot.Services;

public interface ISessionManager
{
  Task<UserSession> GetSessionAsync(long telegramId, CancellationToken cancellationToken = default);
  Task SaveSessionAsync(long telegramId, UserSession session, CancellationToken cancellationToken = default);
  void ClearInactiveSessions();
}

public class SessionManager(IServiceScopeFactory serviceScopeFactory) : ISessionManager
{
  private readonly TimeSpan _inactivityTimeout = TimeSpan.FromHours(24);
  private readonly ConcurrentDictionary<long, UserSession> _sessions = new();

  public async Task<UserSession> GetSessionAsync(long telegramId, CancellationToken cancellationToken = default)
  {
    // Check in-memory cache first
    if (_sessions.TryGetValue(telegramId, out var cachedSession))
      return cachedSession;

    using var scope = serviceScopeFactory.CreateScope();
    var userRepository = scope.ServiceProvider.GetRequiredService<IRepository<User>>();
    var sessionRepository = scope.ServiceProvider.GetRequiredService<IRepository<TelegramSession>>();

    // Get user first
    var user = await userRepository.FirstOrDefaultAsync(new GetUserByTelegramIdSpec(telegramId), cancellationToken);
    if (user == null)
      return _sessions.GetOrAdd(telegramId, _ => new UserSession());

    // Load session from database
    var dbSession = await sessionRepository.FirstOrDefaultAsync(
      new GetTelegramSessionByUserIdSpec(user.Id), cancellationToken);

    UserSession session;
    if (dbSession == null)
    {
      // Create new session in DB
      dbSession = new TelegramSession(user.Id);
      await sessionRepository.AddAsync(dbSession, cancellationToken);
      await sessionRepository.SaveChangesAsync(cancellationToken);

      session = new UserSession();
    }
    else
    {
      session = new UserSession
      {
        CurrentFamilyId = dbSession.CurrentFamilyId,
        State = (ConversationState)dbSession.ConversationState,
        LastActivity = dbSession.LastActivity
      };

      // Deserialize session data if exists
      if (!string.IsNullOrEmpty(dbSession.SessionData))
        try
        {
          session.Data = JsonSerializer.Deserialize<Dictionary<string, object>>(dbSession.SessionData) ??
                         new Dictionary<string, object>();
        }
        catch
        {
          session.Data = new Dictionary<string, object>();
        }
    }

    _sessions[telegramId] = session;
    return session;
  }

  public async Task SaveSessionAsync(long telegramId, UserSession session,
    CancellationToken cancellationToken = default)
  {
    using var scope = serviceScopeFactory.CreateScope();
    var userRepository = scope.ServiceProvider.GetRequiredService<IRepository<User>>();
    var sessionRepository = scope.ServiceProvider.GetRequiredService<IRepository<TelegramSession>>();

    var user = await userRepository.FirstOrDefaultAsync(new GetUserByTelegramIdSpec(telegramId), cancellationToken);
    if (user == null)
      return;

    var dbSession = await sessionRepository.FirstOrDefaultAsync(
      new GetTelegramSessionByUserIdSpec(user.Id), cancellationToken);

    if (dbSession == null)
    {
      // Create new session
      dbSession = new TelegramSession(user.Id);
      await sessionRepository.AddAsync(dbSession, cancellationToken);
    }

    var sessionData = session.Data.Count > 0 ? JsonSerializer.Serialize(session.Data) : null;
    dbSession.UpdateSession(session.CurrentFamilyId, (int)session.State, sessionData);

    await sessionRepository.UpdateAsync(dbSession, cancellationToken);
    await sessionRepository.SaveChangesAsync(cancellationToken);

    // Mark as clean and update cache
    session.MarkClean();
    _sessions[telegramId] = session;
  }

  public void ClearInactiveSessions()
  {
    var now = DateTime.UtcNow;
    var inactiveSessions = _sessions
      .Where(kvp => now - kvp.Value.LastActivity > _inactivityTimeout)
      .Select(kvp => kvp.Key)
      .ToList();

    foreach (var telegramId in inactiveSessions)
    {
      _sessions.TryRemove(telegramId, out _);
    }
  }
}
