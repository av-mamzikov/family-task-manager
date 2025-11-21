using FamilyTaskManager.Bot.Handlers.Commands;
using FamilyTaskManager.Bot.Models;
using FamilyTaskManager.UseCases.Statistics;
using FamilyTaskManager.Core.FamilyAggregate;
using Mediator;
using Telegram.Bot;
using Telegram.Bot.Types;
using Ardalis.Result;

namespace FamilyTaskManager.BotTests.Handlers.Commands;

public class StatsCommandHandlerTests
{
  private readonly IMediator _mediator;
  private readonly ILogger<StatsCommandHandler> _logger;
  private readonly StatsCommandHandler _handler;
  private readonly ITelegramBotClient _botClient;

  public StatsCommandHandlerTests()
  {
    _mediator = Substitute.For<IMediator>();
    _logger = Substitute.For<ILogger<StatsCommandHandler>>();
    _handler = new StatsCommandHandler(_mediator, _logger);
    _botClient = Substitute.For<ITelegramBotClient>();
  }

  [Fact]
  public async Task HandleAsync_ShouldPromptSelectFamily_WhenNoActiveFamilySelected()
  {
    // Arrange
    var message = CreateMessage(chatId: 123);
    var session = new UserSession();
    var userId = Guid.NewGuid();

    // Act
    await _handler.HandleAsync(_botClient, message, session, userId, CancellationToken.None);

    // Assert
    await _botClient.Received(1).SendTextMessageAsync(
      Arg.Is<long>(123),
      Arg.Is<string>(text => text.Contains("выберите активную семью")),
      cancellationToken: Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task HandleAsync_ShouldDisplayDisabledMessage_WhenLeaderboardIsDisabled()
  {
    // Arrange
    var message = CreateMessage(chatId: 123);
    var familyId = Guid.NewGuid();
    var session = new UserSession { CurrentFamilyId = familyId };
    var userId = Guid.NewGuid();

    _mediator.Send(Arg.Any<GetLeaderboardQuery>(), Arg.Any<CancellationToken>())
      .Returns(Result<List<LeaderboardEntryDto>>.Error("Leaderboard is disabled"));

    // Act
    await _handler.HandleAsync(_botClient, message, session, userId, CancellationToken.None);

    // Assert
    await _botClient.Received(1).SendTextMessageAsync(
      Arg.Any<long>(),
      Arg.Is<string>(text => text.Contains("отключён")),
      parseMode: Arg.Any<Telegram.Bot.Types.Enums.ParseMode?>(),
      replyMarkup: Arg.Any<Telegram.Bot.Types.ReplyMarkups.IReplyMarkup>(),
      cancellationToken: Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task HandleAsync_ShouldDisplayLeaderboard_WhenEntriesExist()
  {
    // Arrange
    var message = CreateMessage(chatId: 123);
    var familyId = Guid.NewGuid();
    var session = new UserSession { CurrentFamilyId = familyId };
    var userId = Guid.NewGuid();

    var entries = new List<LeaderboardEntryDto>
    {
      new LeaderboardEntryDto(Guid.NewGuid(), "Alice", 100, FamilyRole.Admin),
      new LeaderboardEntryDto(userId, "Bob", 80, FamilyRole.Adult),
      new LeaderboardEntryDto(Guid.NewGuid(), "Charlie", 50, FamilyRole.Child)
    };

    _mediator.Send(Arg.Any<GetLeaderboardQuery>(), Arg.Any<CancellationToken>())
      .Returns(Result<List<LeaderboardEntryDto>>.Success(entries));

    // Act
    await _handler.HandleAsync(_botClient, message, session, userId, CancellationToken.None);

    // Assert
    await _botClient.Received(1).SendTextMessageAsync(
      Arg.Any<long>(),
      Arg.Is<string>(text => text.Contains("Alice") && text.Contains("Bob") && text.Contains("Charlie")),
      parseMode: Arg.Any<Telegram.Bot.Types.Enums.ParseMode?>(),
      replyMarkup: Arg.Any<Telegram.Bot.Types.ReplyMarkups.IReplyMarkup>(),
      cancellationToken: Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task HandleAsync_ShouldShowMedals_ForTopThreePlaces()
  {
    // Arrange
    var message = CreateMessage(chatId: 123);
    var familyId = Guid.NewGuid();
    var session = new UserSession { CurrentFamilyId = familyId };
    var userId = Guid.NewGuid();

    var entries = new List<LeaderboardEntryDto>
    {
      new LeaderboardEntryDto(Guid.NewGuid(), "First", 100, FamilyRole.Admin),
      new LeaderboardEntryDto(Guid.NewGuid(), "Second", 80, FamilyRole.Adult),
      new LeaderboardEntryDto(Guid.NewGuid(), "Third", 60, FamilyRole.Child)
    };

    _mediator.Send(Arg.Any<GetLeaderboardQuery>(), Arg.Any<CancellationToken>())
      .Returns(Result<List<LeaderboardEntryDto>>.Success(entries));

    // Act
    await _handler.HandleAsync(_botClient, message, session, userId, CancellationToken.None);

    // Assert
    await _botClient.Received(1).SendTextMessageAsync(
      Arg.Any<long>(),
      Arg.Is<string>(text => text.Contains("🥇") && text.Contains("🥈") && text.Contains("🥉")),
      parseMode: Arg.Any<Telegram.Bot.Types.Enums.ParseMode?>(),
      replyMarkup: Arg.Any<Telegram.Bot.Types.ReplyMarkups.IReplyMarkup>(),
      cancellationToken: Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task HandleAsync_ShouldHighlightCurrentUser_InLeaderboard()
  {
    // Arrange
    var message = CreateMessage(chatId: 123);
    var familyId = Guid.NewGuid();
    var session = new UserSession { CurrentFamilyId = familyId };
    var userId = Guid.NewGuid();

    var entries = new List<LeaderboardEntryDto>
    {
      new LeaderboardEntryDto(Guid.NewGuid(), "Alice", 100, FamilyRole.Admin),
      new LeaderboardEntryDto(userId, "CurrentUser", 80, FamilyRole.Adult)
    };

    _mediator.Send(Arg.Any<GetLeaderboardQuery>(), Arg.Any<CancellationToken>())
      .Returns(Result<List<LeaderboardEntryDto>>.Success(entries));

    // Act
    await _handler.HandleAsync(_botClient, message, session, userId, CancellationToken.None);

    // Assert
    await _botClient.Received(1).SendTextMessageAsync(
      Arg.Any<long>(),
      Arg.Is<string>(text => text.Contains("➡️")),
      parseMode: Arg.Any<Telegram.Bot.Types.Enums.ParseMode?>(),
      replyMarkup: Arg.Any<Telegram.Bot.Types.ReplyMarkups.IReplyMarkup>(),
      cancellationToken: Arg.Any<CancellationToken>());
  }

  private static Message CreateMessage(long chatId, string text = "/stats")
  {
    return new Message
    {
      MessageId = 1,
      Chat = new Chat { Id = chatId },
      From = new User { Id = chatId, FirstName = "Test" },
      Text = text,
      Date = DateTime.UtcNow
    };
  }
}
