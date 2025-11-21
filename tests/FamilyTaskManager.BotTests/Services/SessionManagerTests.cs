using FamilyTaskManager.Bot.Services;
using FamilyTaskManager.Bot.Models;

namespace FamilyTaskManager.BotTests.Services;

public class SessionManagerTests
{
  private readonly SessionManager _sessionManager;

  public SessionManagerTests()
  {
    _sessionManager = new SessionManager();
  }

  [Fact]
  public void GetSession_ShouldCreateNewSession_WhenNotExists()
  {
    // Arrange
    var telegramId = 12345L;

    // Act
    var session = _sessionManager.GetSession(telegramId);

    // Assert
    session.ShouldNotBeNull();
    session.State.ShouldBe(ConversationState.None);
    session.CurrentFamilyId.ShouldBeNull();
    session.Data.ShouldBeEmpty();
  }

  [Fact]
  public void GetSession_ShouldReturnSameSession_WhenCalledMultipleTimes()
  {
    // Arrange
    var telegramId = 12345L;

    // Act
    var session1 = _sessionManager.GetSession(telegramId);
    session1.CurrentFamilyId = Guid.NewGuid();
    
    var session2 = _sessionManager.GetSession(telegramId);

    // Assert
    session1.ShouldBe(session2);
    session2.CurrentFamilyId.ShouldBe(session1.CurrentFamilyId);
  }

  [Fact]
  public void GetSession_ShouldReturnDifferentSessions_ForDifferentUsers()
  {
    // Arrange
    var telegramId1 = 12345L;
    var telegramId2 = 67890L;

    // Act
    var session1 = _sessionManager.GetSession(telegramId1);
    var session2 = _sessionManager.GetSession(telegramId2);

    // Assert
    session1.ShouldNotBe(session2);
  }

  [Fact]
  public void ClearInactiveSessions_ShouldRemoveOldSessions()
  {
    // Arrange
    var telegramId = 12345L;
    var session = _sessionManager.GetSession(telegramId);
    
    // Simulate old session (>24 hours)
    session.LastActivity = DateTime.UtcNow.AddHours(-25);

    // Act
    _sessionManager.ClearInactiveSessions();
    var newSession = _sessionManager.GetSession(telegramId);

    // Assert
    newSession.ShouldNotBe(session);
    newSession.LastActivity.ShouldBeGreaterThan(session.LastActivity);
  }

  [Fact]
  public void ClearInactiveSessions_ShouldKeepActiveSessions()
  {
    // Arrange
    var telegramId = 12345L;
    var session = _sessionManager.GetSession(telegramId);
    var familyId = Guid.NewGuid();
    session.CurrentFamilyId = familyId;

    // Act
    _sessionManager.ClearInactiveSessions();
    var sameSession = _sessionManager.GetSession(telegramId);

    // Assert
    sameSession.CurrentFamilyId.ShouldBe(familyId);
  }
}
