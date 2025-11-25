using Ardalis.Result;
using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Host.Modules.Bot.Handlers.Commands;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.UseCases.Statistics;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;

namespace FamilyTaskManager.UnitTests.Host.Bot.Handlers.Commands;

public class StatsCommandHandlerTests
{
  private readonly ITelegramBotClient _botClient;
  private readonly StatsCommandHandler _handler;
  private readonly IMediator _mediator;

  public StatsCommandHandlerTests()
  {
    _mediator = Substitute.For<IMediator>();
    _handler = new StatsCommandHandler(_mediator);
    _botClient = Substitute.For<ITelegramBotClient>();
  }

  [Fact]
  public async Task HandleAsync_ShouldPromptSelectFamily_WhenNoActiveFamilySelected()
  {
    // Arrange
    var message = CreateMessage(123);
    var session = new UserSession();
    var userId = Guid.NewGuid();

    // Act
    await _handler.HandleAsync(_botClient, message, session, userId, CancellationToken.None);

    // Assert
    await _botClient.Received(1).MakeRequestAsync(
      Arg.Is<SendMessageRequest>(req => req.ChatId.Identifier == 123 && req.Text.Contains("выберите активную семью")),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task HandleAsync_ShouldDisplayDisabledMessage_WhenLeaderboardIsDisabled()
  {
    // Arrange
    var message = CreateMessage(123);
    var familyId = Guid.NewGuid();
    var session = new UserSession { CurrentFamilyId = familyId };
    var userId = Guid.NewGuid();

    _mediator.Send(Arg.Any<GetLeaderboardQuery>(), Arg.Any<CancellationToken>())
      .Returns(Result<List<LeaderboardEntryDto>>.Error("Leaderboard is disabled"));

    // Act
    await _handler.HandleAsync(_botClient, message, session, userId, CancellationToken.None);

    // Assert
    await _botClient.Received(1).MakeRequestAsync(
      Arg.Is<SendMessageRequest>(req => req.Text.Contains("отключён")),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task HandleAsync_ShouldDisplayLeaderboard_WhenEntriesExist()
  {
    // Arrange
    var message = CreateMessage(123);
    var familyId = Guid.NewGuid();
    var session = new UserSession { CurrentFamilyId = familyId };
    var userId = Guid.NewGuid();

    var entries = new List<LeaderboardEntryDto>
    {
      new(Guid.NewGuid(), "Alice", 100, FamilyRole.Admin),
      new(userId, "Bob", 80, FamilyRole.Adult),
      new(Guid.NewGuid(), "Charlie", 50, FamilyRole.Child)
    };

    _mediator.Send(Arg.Any<GetLeaderboardQuery>(), Arg.Any<CancellationToken>())
      .Returns(Result<List<LeaderboardEntryDto>>.Success(entries));

    // Act
    await _handler.HandleAsync(_botClient, message, session, userId, CancellationToken.None);

    // Assert
    await _botClient.Received(1).MakeRequestAsync(
      Arg.Is<SendMessageRequest>(req =>
        req.Text.Contains("Alice") && req.Text.Contains("Bob") && req.Text.Contains("Charlie")),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task HandleAsync_ShouldShowMedals_ForTopThreePlaces()
  {
    // Arrange
    var message = CreateMessage(123);
    var familyId = Guid.NewGuid();
    var session = new UserSession { CurrentFamilyId = familyId };
    var userId = Guid.NewGuid();

    var entries = new List<LeaderboardEntryDto>
    {
      new(Guid.NewGuid(), "First", 100, FamilyRole.Admin),
      new(Guid.NewGuid(), "Second", 80, FamilyRole.Adult),
      new(Guid.NewGuid(), "Third", 60, FamilyRole.Child)
    };

    _mediator.Send(Arg.Any<GetLeaderboardQuery>(), Arg.Any<CancellationToken>())
      .Returns(Result<List<LeaderboardEntryDto>>.Success(entries));

    // Act
    await _handler.HandleAsync(_botClient, message, session, userId, CancellationToken.None);

    // Assert
    await _botClient.Received(1).MakeRequestAsync(
      Arg.Is<SendMessageRequest>(req => req.Text.Contains("🥇") && req.Text.Contains("🥈") && req.Text.Contains("🥉")),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task HandleAsync_ShouldHighlightCurrentUser_InLeaderboard()
  {
    // Arrange
    var message = CreateMessage(123);
    var familyId = Guid.NewGuid();
    var session = new UserSession { CurrentFamilyId = familyId };
    var userId = Guid.NewGuid();

    var entries = new List<LeaderboardEntryDto>
    {
      new(Guid.NewGuid(), "Alice", 100, FamilyRole.Admin), new(userId, "CurrentUser", 80, FamilyRole.Adult)
    };

    _mediator.Send(Arg.Any<GetLeaderboardQuery>(), Arg.Any<CancellationToken>())
      .Returns(Result<List<LeaderboardEntryDto>>.Success(entries));

    // Act
    await _handler.HandleAsync(_botClient, message, session, userId, CancellationToken.None);

    // Assert
    await _botClient.Received(1).MakeRequestAsync(
      Arg.Is<SendMessageRequest>(req => req.Text.Contains("➡️")),
      Arg.Any<CancellationToken>());
  }

  private static Message CreateMessage(long chatId, string text = "/stats") =>
    new()
    {
      MessageId = 1,
      Chat = new Chat { Id = chatId },
      From = new User { Id = chatId, FirstName = "Test" },
      Text = text,
      Date = DateTime.UtcNow
    };
}
