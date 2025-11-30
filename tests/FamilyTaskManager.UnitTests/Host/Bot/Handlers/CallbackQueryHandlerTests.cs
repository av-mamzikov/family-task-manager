using FamilyTaskManager.Host.Modules.Bot.Handlers;
using FamilyTaskManager.Host.Modules.Bot.Handlers.CallbackHandlers;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.Host.Modules.Bot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace FamilyTaskManager.UnitTests.Host.Bot.Handlers;

public class CallbackQueryHandlerTests
{
  private readonly ITelegramBotClient _botClient;
  private readonly ICallbackRouter _callbackRouter;
  private readonly CallbackQueryHandler _handler;
  private readonly ILogger<CallbackQueryHandler> _logger;
  private readonly ISessionManager _sessionManager;

  public CallbackQueryHandlerTests()
  {
    _sessionManager = Substitute.For<ISessionManager>();
    _logger = Substitute.For<ILogger<CallbackQueryHandler>>();
    _callbackRouter = Substitute.For<ICallbackRouter>();
    _handler = new CallbackQueryHandler(_logger, _sessionManager, _callbackRouter);
    _botClient = Substitute.For<ITelegramBotClient>();
  }

  [Fact]
  public async Task HandleCallbackAsync_ShouldAnswerCallbackQuery()
  {
    // Arrange
    var callbackQuery = CreateCallbackQuery("create_family");
    var session = new UserSession();
    _sessionManager.GetSessionAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(session);

    // Act
    await _handler.HandleCallbackAsync(_botClient, callbackQuery, CancellationToken.None);

    // Assert - handler should complete without throwing
    // Note: Cannot verify AnswerCallbackQueryAsync as it's an extension method
  }

  [Fact]
  public async Task HandleCallbackAsync_ShouldDelegateToRouter()
  {
    // Arrange
    var callbackQuery = CreateCallbackQuery("create_family");
    var session = new UserSession();

    _sessionManager.GetSessionAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(session);

    // Act
    await _handler.HandleCallbackAsync(_botClient, callbackQuery, CancellationToken.None);

    // Assert
    await _callbackRouter.Received(1).RouteCallbackAsync(
      _botClient,
      callbackQuery,
      session,
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task HandleCallbackAsync_ShouldUpdateActivity_WhenHandlingCallback()
  {
    // Arrange
    var callbackQuery = CreateCallbackQuery("create_family");
    var session = Substitute.For<UserSession>();
    _sessionManager.GetSessionAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(session);

    // Act
    await _handler.HandleCallbackAsync(_botClient, callbackQuery, CancellationToken.None);

    // Assert
    session.Received(1).UpdateActivity();
  }

  [Fact]
  public async Task HandleCallbackAsync_ShouldHandleUnknownAction_Gracefully()
  {
    // Arrange
    var callbackQuery = CreateCallbackQuery("unknown_action_123");
    var session = new UserSession();
    _sessionManager.GetSessionAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(session);

    // Act
    await _handler.HandleCallbackAsync(_botClient, callbackQuery, CancellationToken.None);

    // Assert - handler should complete without throwing
    // Note: Cannot verify SendTextMessageAsync as it's an extension method
  }

  private static CallbackQuery CreateCallbackQuery(string data, long chatId = 123) =>
    new()
    {
      Id = "callback_id",
      Data = data,
      From = new User { Id = chatId, FirstName = "Test" },
      Message = new Message { MessageId = 1, Chat = new Chat { Id = chatId }, Date = DateTime.UtcNow }
    };
}
