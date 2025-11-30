using FamilyTaskManager.Core.UserAggregate;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.Host.Modules.Bot.Services;
using FamilyTaskManager.UseCases.Users.Specifications;

namespace FamilyTaskManager.UnitTests.Host.Bot.Services;

public class SessionManagerTests
{
  private readonly IServiceScopeFactory _serviceScopeFactory;
  private readonly SessionManager _sessionManager;
  private readonly IRepository<TelegramSession> _sessionRepository;
  private readonly IRepository<User> _userRepository;

  public SessionManagerTests()
  {
    _userRepository = Substitute.For<IRepository<User>>();
    _sessionRepository = Substitute.For<IRepository<TelegramSession>>();

    // Setup service scope factory
    var serviceProvider = Substitute.For<IServiceProvider>();
    serviceProvider.GetService(typeof(IRepository<User>)).Returns(_userRepository);
    serviceProvider.GetService(typeof(IRepository<TelegramSession>)).Returns(_sessionRepository);

    var scope = Substitute.For<IServiceScope>();
    scope.ServiceProvider.Returns(serviceProvider);

    _serviceScopeFactory = Substitute.For<IServiceScopeFactory>();
    _serviceScopeFactory.CreateScope().Returns(scope);

    _sessionManager = new SessionManager(_serviceScopeFactory);
  }

  [Fact]
  public async Task GetSessionAsync_ShouldCreateNewSession_WhenUserNotExists()
  {
    // Arrange
    var telegramId = 12345L;
    _userRepository.FirstOrDefaultAsync(Arg.Any<GetUserByTelegramIdSpec>(), Arg.Any<CancellationToken>())
      .Returns((User?)null);

    // Act
    var session = await _sessionManager.GetSessionAsync(telegramId);

    // Assert
    session.ShouldNotBeNull();
    session.State.ShouldBe(ConversationState.None);
    session.CurrentFamilyId.ShouldBeNull();
    session.Data.ShouldBeEmpty();
  }

  [Fact]
  public async Task GetSessionAsync_ShouldReturnCachedSession_WhenCalledMultipleTimes()
  {
    // Arrange
    var telegramId = 12345L;
    _userRepository.FirstOrDefaultAsync(Arg.Any<GetUserByTelegramIdSpec>(), Arg.Any<CancellationToken>())
      .Returns((User?)null);

    // Act
    var session1 = await _sessionManager.GetSessionAsync(telegramId);
    session1.CurrentFamilyId = Guid.NewGuid();

    var session2 = await _sessionManager.GetSessionAsync(telegramId);

    // Assert
    session1.ShouldBe(session2);
    session2.CurrentFamilyId.ShouldBe(session1.CurrentFamilyId);
  }

  [Fact]
  public async Task GetSessionAsync_ShouldReturnDifferentSessions_ForDifferentUsers()
  {
    // Arrange
    var telegramId1 = 12345L;
    var telegramId2 = 67890L;
    _userRepository.FirstOrDefaultAsync(Arg.Any<GetUserByTelegramIdSpec>(), Arg.Any<CancellationToken>())
      .Returns((User?)null);

    // Act
    var session1 = await _sessionManager.GetSessionAsync(telegramId1);
    var session2 = await _sessionManager.GetSessionAsync(telegramId2);

    // Assert
    session1.ShouldNotBe(session2);
  }

  [Fact]
  public async Task ClearInactiveSessions_ShouldRemoveOldSessions()
  {
    // Arrange
    var telegramId = 12345L;
    _userRepository.FirstOrDefaultAsync(Arg.Any<GetUserByTelegramIdSpec>(), Arg.Any<CancellationToken>())
      .Returns((User?)null);

    var session = await _sessionManager.GetSessionAsync(telegramId);

    // Simulate old session (>24 hours)
    session.LastActivity = DateTime.UtcNow.AddHours(-25);

    // Act
    _sessionManager.ClearInactiveSessions();
    var newSession = await _sessionManager.GetSessionAsync(telegramId);

    // Assert
    newSession.ShouldNotBe(session);
    newSession.LastActivity.ShouldBeGreaterThan(session.LastActivity);
  }

  [Fact]
  public async Task ClearInactiveSessions_ShouldKeepActiveSessions()
  {
    // Arrange
    var telegramId = 12345L;
    _userRepository.FirstOrDefaultAsync(Arg.Any<GetUserByTelegramIdSpec>(), Arg.Any<CancellationToken>())
      .Returns((User?)null);

    var session = await _sessionManager.GetSessionAsync(telegramId);
    var familyId = Guid.NewGuid();
    session.CurrentFamilyId = familyId;

    // Act
    _sessionManager.ClearInactiveSessions();
    var sameSession = await _sessionManager.GetSessionAsync(telegramId);

    // Assert
    sameSession.CurrentFamilyId.ShouldBe(familyId);
  }
}
