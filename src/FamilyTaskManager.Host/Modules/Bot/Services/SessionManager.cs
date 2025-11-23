using System.Collections.Concurrent;
using FamilyTaskManager.Host.Modules.Bot.Models;

namespace FamilyTaskManager.Host.Modules.Bot.Services;

public interface ISessionManager
{
  UserSession GetSession(long telegramId);
  void ClearInactiveSessions();
}

public class SessionManager : ISessionManager
{
  private readonly TimeSpan _inactivityTimeout = TimeSpan.FromHours(24);
  private readonly ConcurrentDictionary<long, UserSession> _sessions = new();

  public UserSession GetSession(long telegramId)
  {
    return _sessions.GetOrAdd(telegramId, _ => new UserSession());
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
