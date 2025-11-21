using FamilyTaskManager.Bot.Handlers.Commands;
using FamilyTaskManager.Bot.Models;
using FamilyTaskManager.UseCases.Pets;
using FamilyTaskManager.Core.PetAggregate;
using Mediator;
using Telegram.Bot;
using Telegram.Bot.Types;
using Ardalis.Result;

namespace FamilyTaskManager.BotTests.Handlers.Commands;

public class PetCommandHandlerTests
{
  private readonly IMediator _mediator;
  private readonly ILogger<PetCommandHandler> _logger;
  private readonly PetCommandHandler _handler;
  private readonly ITelegramBotClient _botClient;

  public PetCommandHandlerTests()
  {
    _mediator = Substitute.For<IMediator>();
    _logger = Substitute.For<ILogger<PetCommandHandler>>();
    _handler = new PetCommandHandler(_mediator, _logger);
    _botClient = Substitute.For<ITelegramBotClient>();
  }

  [Fact]
  public async Task HandleAsync_ShouldPromptSelectFamily_WhenNoActiveFamilySelected()
  {
    // Arrange
    var message = CreateMessage(chatId: 123);
    var session = new UserSession(); // No CurrentFamilyId
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
  public async Task HandleAsync_ShouldDisplayNoPetsMessage_WhenNoPetsExist()
  {
    // Arrange
    var message = CreateMessage(chatId: 123);
    var familyId = Guid.NewGuid();
    var session = new UserSession { CurrentFamilyId = familyId };
    var userId = Guid.NewGuid();

    _mediator.Send(Arg.Any<GetPetsQuery>(), Arg.Any<CancellationToken>())
      .Returns(Result<List<PetDto>>.Success(new List<PetDto>()));

    // Act
    await _handler.HandleAsync(_botClient, message, session, userId, CancellationToken.None);

    // Assert
    await _botClient.Received(1).SendTextMessageAsync(
      Arg.Is<long>(123),
      Arg.Is<string>(text => text.Contains("нет питомцев")),
      replyMarkup: Arg.Any<Telegram.Bot.Types.ReplyMarkups.IReplyMarkup>(),
      cancellationToken: Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task HandleAsync_ShouldDisplayPets_WhenPetsExist()
  {
    // Arrange
    var message = CreateMessage(chatId: 123);
    var familyId = Guid.NewGuid();
    var session = new UserSession { CurrentFamilyId = familyId };
    var userId = Guid.NewGuid();

    var pets = new List<PetDto>
    {
      new PetDto(Guid.NewGuid(), "Fluffy", PetType.Cat, 85),
      new PetDto(Guid.NewGuid(), "Rex", PetType.Dog, 60)
    };

    _mediator.Send(Arg.Any<GetPetsQuery>(), Arg.Any<CancellationToken>())
      .Returns(Result<List<PetDto>>.Success(pets));

    // Act
    await _handler.HandleAsync(_botClient, message, session, userId, CancellationToken.None);

    // Assert
    await _botClient.Received(1).SendTextMessageAsync(
      Arg.Any<long>(),
      Arg.Is<string>(text => 
        text.Contains("Fluffy") && 
        text.Contains("Rex") &&
        text.Contains("🐱") &&
        text.Contains("🐶")),
      parseMode: Arg.Any<Telegram.Bot.Types.Enums.ParseMode?>(),
      replyMarkup: Arg.Any<Telegram.Bot.Types.ReplyMarkups.IReplyMarkup>(),
      cancellationToken: Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task HandleAsync_ShouldShowHappyMoodEmoji_WhenMoodScoreIsHigh()
  {
    // Arrange
    var message = CreateMessage(chatId: 123);
    var familyId = Guid.NewGuid();
    var session = new UserSession { CurrentFamilyId = familyId };
    var userId = Guid.NewGuid();

    var pets = new List<PetDto>
    {
      new PetDto(Guid.NewGuid(), "Happy Cat", PetType.Cat, 90) // High mood
    };

    _mediator.Send(Arg.Any<GetPetsQuery>(), Arg.Any<CancellationToken>())
      .Returns(Result<List<PetDto>>.Success(pets));

    // Act
    await _handler.HandleAsync(_botClient, message, session, userId, CancellationToken.None);

    // Assert
    await _botClient.Received(1).SendTextMessageAsync(
      Arg.Any<long>(),
      Arg.Is<string>(text => text.Contains("😊") && text.Contains("Отлично")),
      parseMode: Arg.Any<Telegram.Bot.Types.Enums.ParseMode?>(),
      replyMarkup: Arg.Any<Telegram.Bot.Types.ReplyMarkups.IReplyMarkup>(),
      cancellationToken: Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task HandleAsync_ShouldShowSadMoodEmoji_WhenMoodScoreIsLow()
  {
    // Arrange
    var message = CreateMessage(chatId: 123);
    var familyId = Guid.NewGuid();
    var session = new UserSession { CurrentFamilyId = familyId };
    var userId = Guid.NewGuid();

    var pets = new List<PetDto>
    {
      new PetDto(Guid.NewGuid(), "Sad Cat", PetType.Cat, 15) // Low mood
    };

    _mediator.Send(Arg.Any<GetPetsQuery>(), Arg.Any<CancellationToken>())
      .Returns(Result<List<PetDto>>.Success(pets));

    // Act
    await _handler.HandleAsync(_botClient, message, session, userId, CancellationToken.None);

    // Assert
    await _botClient.Received(1).SendTextMessageAsync(
      Arg.Any<long>(),
      Arg.Is<string>(text => text.Contains("😢") && text.Contains("Очень грустно")),
      parseMode: Arg.Any<Telegram.Bot.Types.Enums.ParseMode?>(),
      replyMarkup: Arg.Any<Telegram.Bot.Types.ReplyMarkups.IReplyMarkup>(),
      cancellationToken: Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task HandleAsync_ShouldDisplayCorrectPetTypeText_ForAllPetTypes()
  {
    // Arrange
    var message = CreateMessage(chatId: 123);
    var familyId = Guid.NewGuid();
    var session = new UserSession { CurrentFamilyId = familyId };
    var userId = Guid.NewGuid();

    var pets = new List<PetDto>
    {
      new PetDto(Guid.NewGuid(), "Cat", PetType.Cat, 50),
      new PetDto(Guid.NewGuid(), "Dog", PetType.Dog, 50),
      new PetDto(Guid.NewGuid(), "Hamster", PetType.Hamster, 50)
    };

    _mediator.Send(Arg.Any<GetPetsQuery>(), Arg.Any<CancellationToken>())
      .Returns(Result<List<PetDto>>.Success(pets));

    // Act
    await _handler.HandleAsync(_botClient, message, session, userId, CancellationToken.None);

    // Assert
    await _botClient.Received(1).SendTextMessageAsync(
      Arg.Any<long>(),
      Arg.Is<string>(text => 
        text.Contains("Кот") && 
        text.Contains("Собака") && 
        text.Contains("Хомяк")),
      parseMode: Arg.Any<Telegram.Bot.Types.Enums.ParseMode?>(),
      replyMarkup: Arg.Any<Telegram.Bot.Types.ReplyMarkups.IReplyMarkup>(),
      cancellationToken: Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task HandleAsync_ShouldHandleError_WhenGetPetsFails()
  {
    // Arrange
    var message = CreateMessage(chatId: 123);
    var familyId = Guid.NewGuid();
    var session = new UserSession { CurrentFamilyId = familyId };
    var userId = Guid.NewGuid();

    _mediator.Send(Arg.Any<GetPetsQuery>(), Arg.Any<CancellationToken>())
      .Returns(Result<List<PetDto>>.Error("Database error"));

    // Act
    await _handler.HandleAsync(_botClient, message, session, userId, CancellationToken.None);

    // Assert
    await _botClient.Received(1).SendTextMessageAsync(
      Arg.Is<long>(123),
      Arg.Is<string>(text => text.Contains("Ошибка")),
      cancellationToken: Arg.Any<CancellationToken>());
  }

  private static Message CreateMessage(long chatId, string text = "/pet")
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
