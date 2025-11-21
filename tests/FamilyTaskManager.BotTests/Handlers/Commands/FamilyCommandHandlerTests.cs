using FamilyTaskManager.Bot.Handlers.Commands;
using FamilyTaskManager.Bot.Models;
using FamilyTaskManager.UseCases.Families;
using FamilyTaskManager.Core.FamilyAggregate;
using Mediator;
using Telegram.Bot;
using Telegram.Bot.Types;
using Ardalis.Result;

namespace FamilyTaskManager.BotTests.Handlers.Commands;

public class FamilyCommandHandlerTests
{
  private readonly IMediator _mediator;
  private readonly ILogger<FamilyCommandHandler> _logger;
  private readonly FamilyCommandHandler _handler;
  private readonly ITelegramBotClient _botClient;

  public FamilyCommandHandlerTests()
  {
    _mediator = Substitute.For<IMediator>();
    _logger = Substitute.For<ILogger<FamilyCommandHandler>>();
    _handler = new FamilyCommandHandler(_mediator, _logger);
    _botClient = Substitute.For<ITelegramBotClient>();
  }

  [Fact]
  public async Task HandleAsync_ShouldSendCreateFamilyPrompt_WhenUserHasNoFamilies()
  {
    // Arrange
    var message = CreateMessage(chatId: 123);
    var session = new UserSession();
    var userId = Guid.NewGuid();

    _mediator.Send(Arg.Any<GetUserFamiliesQuery>(), Arg.Any<CancellationToken>())
      .Returns(Result<List<FamilyDto>>.Success(new List<FamilyDto>()));

    // Act
    await _handler.HandleAsync(_botClient, message, session, userId, CancellationToken.None);

    // Assert
    await _botClient.Received(1).SendTextMessageAsync(
      Arg.Is<long>(123),
      Arg.Is<string>(text => text.Contains("нет семей")),
      parseMode: Arg.Any<Telegram.Bot.Types.Enums.ParseMode?>(),
      replyMarkup: Arg.Any<Telegram.Bot.Types.ReplyMarkups.IReplyMarkup>(),
      cancellationToken: Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task HandleAsync_ShouldDisplayFamilies_WhenUserHasFamilies()
  {
    // Arrange
    var message = CreateMessage(chatId: 123);
    var session = new UserSession();
    var userId = Guid.NewGuid();
    var familyId = Guid.NewGuid();

    var families = new List<FamilyDto>
    {
      new FamilyDto(familyId, "Test Family", "UTC", true, FamilyRole.Admin, 100)
    };

    _mediator.Send(Arg.Any<GetUserFamiliesQuery>(), Arg.Any<CancellationToken>())
      .Returns(Result<List<FamilyDto>>.Success(families));

    // Act
    await _handler.HandleAsync(_botClient, message, session, userId, CancellationToken.None);

    // Assert
    await _botClient.Received(1).SendTextMessageAsync(
      Arg.Is<long>(123),
      Arg.Is<string>(text => text.Contains("Test Family") && text.Contains("Администратор")),
      parseMode: Arg.Any<Telegram.Bot.Types.Enums.ParseMode?>(),
      replyMarkup: Arg.Any<Telegram.Bot.Types.ReplyMarkups.IReplyMarkup>(),
      cancellationToken: Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task HandleAsync_ShouldShowAdminButtons_WhenUserIsAdmin()
  {
    // Arrange
    var message = CreateMessage(chatId: 123);
    var familyId = Guid.NewGuid();
    var session = new UserSession { CurrentFamilyId = familyId };
    var userId = Guid.NewGuid();

    var families = new List<FamilyDto>
    {
      new FamilyDto(familyId, "Test Family", "UTC", true, FamilyRole.Admin, 100)
    };

    _mediator.Send(Arg.Any<GetUserFamiliesQuery>(), Arg.Any<CancellationToken>())
      .Returns(Result<List<FamilyDto>>.Success(families));

    // Act
    await _handler.HandleAsync(_botClient, message, session, userId, CancellationToken.None);

    // Assert
    await _botClient.Received(1).SendTextMessageAsync(
      Arg.Any<long>(),
      Arg.Any<string>(),
      parseMode: Arg.Any<Telegram.Bot.Types.Enums.ParseMode?>(),
      replyMarkup: Arg.Is<Telegram.Bot.Types.ReplyMarkups.IReplyMarkup>(
        markup => markup.ToString()!.Contains("Управление участниками")),
      cancellationToken: Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task HandleAsync_ShouldNotShowAdminButtons_WhenUserIsNotAdmin()
  {
    // Arrange
    var message = CreateMessage(chatId: 123);
    var familyId = Guid.NewGuid();
    var session = new UserSession { CurrentFamilyId = familyId };
    var userId = Guid.NewGuid();

    var families = new List<FamilyDto>
    {
      new FamilyDto(familyId, "Test Family", "UTC", true, FamilyRole.Child, 50)
    };

    _mediator.Send(Arg.Any<GetUserFamiliesQuery>(), Arg.Any<CancellationToken>())
      .Returns(Result<List<FamilyDto>>.Success(families));

    // Act
    await _handler.HandleAsync(_botClient, message, session, userId, CancellationToken.None);

    // Assert
    await _botClient.Received(1).SendTextMessageAsync(
      Arg.Any<long>(),
      Arg.Any<string>(),
      parseMode: Arg.Any<Telegram.Bot.Types.Enums.ParseMode?>(),
      replyMarkup: Arg.Is<Telegram.Bot.Types.ReplyMarkups.IReplyMarkup>(
        markup => !markup.ToString()!.Contains("Управление участниками")),
      cancellationToken: Arg.Any<CancellationToken>());
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
