using Ardalis.Result;
using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Host.Modules.Bot.Handlers.Commands;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.UseCases.Families;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;

namespace FamilyTaskManager.UnitTests.Host.Bot.Handlers.Commands;

public class FamilyCommandHandlerTests
{
  private readonly ITelegramBotClient _botClient;
  private readonly FamilyCommandHandler _handler;
  private readonly IMediator _mediator;

  public FamilyCommandHandlerTests()
  {
    _mediator = Substitute.For<IMediator>();
    _handler = new FamilyCommandHandler(_mediator);
    _botClient = Substitute.For<ITelegramBotClient>();
  }

  [Fact]
  public async Task HandleAsync_ShouldSendCreateFamilyPrompt_WhenUserHasNoFamilies()
  {
    // Arrange
    var message = CreateMessage(123);
    var session = new UserSession();
    var userId = Guid.NewGuid();

    _mediator.Send(Arg.Any<GetUserFamiliesQuery>(), Arg.Any<CancellationToken>())
      .Returns(Result<List<FamilyDto>>.Success(new List<FamilyDto>()));

    // Act
    await _handler.HandleAsync(_botClient, message, session, userId, CancellationToken.None);

    // Assert
    await _botClient.Received(1).MakeRequestAsync(
      Arg.Is<SendMessageRequest>(req => req.ChatId.Identifier == 123 && req.Text.Contains("нет семей")),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task HandleAsync_ShouldDisplayFamilies_WhenUserHasFamilies()
  {
    // Arrange
    var message = CreateMessage(123);
    var session = new UserSession();
    var userId = Guid.NewGuid();
    var familyId = Guid.NewGuid();

    var families = new List<FamilyDto> { new(familyId, "Test Family", "UTC", true, FamilyRole.Admin, 100) };

    _mediator.Send(Arg.Any<GetUserFamiliesQuery>(), Arg.Any<CancellationToken>())
      .Returns(Result<List<FamilyDto>>.Success(families));

    // Act
    await _handler.HandleAsync(_botClient, message, session, userId, CancellationToken.None);

    // Assert
    await _botClient.Received(1).MakeRequestAsync(
      Arg.Is<SendMessageRequest>(req =>
        req.ChatId.Identifier == 123 && req.Text.Contains("Test Family") && req.Text.Contains("Администратор")),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task HandleAsync_ShouldShowAdminButtons_WhenUserIsAdmin()
  {
    // Arrange
    var message = CreateMessage(123);
    var familyId = Guid.NewGuid();
    var session = new UserSession { CurrentFamilyId = familyId };
    var userId = Guid.NewGuid();

    var families = new List<FamilyDto> { new(familyId, "Test Family", "UTC", true, FamilyRole.Admin, 100) };

    _mediator.Send(Arg.Any<GetUserFamiliesQuery>(), Arg.Any<CancellationToken>())
      .Returns(Result<List<FamilyDto>>.Success(families));

    // Act
    await _handler.HandleAsync(_botClient, message, session, userId, CancellationToken.None);

    // Assert
    await _botClient.Received(1).MakeRequestAsync(
      Arg.Is<SendMessageRequest>(req => req.ChatId.Identifier == 123),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task HandleAsync_ShouldNotShowAdminButtons_WhenUserIsNotAdmin()
  {
    // Arrange
    var message = CreateMessage(123);
    var familyId = Guid.NewGuid();
    var session = new UserSession { CurrentFamilyId = familyId };
    var userId = Guid.NewGuid();

    var families = new List<FamilyDto> { new(familyId, "Test Family", "UTC", true, FamilyRole.Child, 50) };

    _mediator.Send(Arg.Any<GetUserFamiliesQuery>(), Arg.Any<CancellationToken>())
      .Returns(Result<List<FamilyDto>>.Success(families));

    // Act
    await _handler.HandleAsync(_botClient, message, session, userId, CancellationToken.None);

    // Assert
    await _botClient.Received(1).MakeRequestAsync(
      Arg.Is<SendMessageRequest>(req => req.ChatId.Identifier == 123),
      Arg.Any<CancellationToken>());
  }

  private static Message CreateMessage(long chatId, string text = "/family")
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
