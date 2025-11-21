using FamilyTaskManager.Host.Modules.Bot.Handlers.Commands;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.UseCases.Pets;
using FamilyTaskManager.Core.PetAggregate;
using Mediator;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Requests;
using Ardalis.Result;

namespace FamilyTaskManager.UnitTests.Host.Bot.Handlers.Commands;

public class PetCommandHandlerTests
{
  private readonly IMediator _mediator;
  private readonly PetCommandHandler _handler;
  private readonly ITelegramBotClient _botClient;

  public PetCommandHandlerTests()
  {
    _mediator = Substitute.For<IMediator>();
    _handler = new PetCommandHandler(_mediator);
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
    await _botClient.Received(1).MakeRequestAsync(
      Arg.Is<SendMessageRequest>(req => req.ChatId.Identifier == 123 && req.Text.Contains("выберите активную семью")),
      Arg.Any<CancellationToken>());
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
    await _botClient.Received(1).MakeRequestAsync(
      Arg.Is<SendMessageRequest>(req => req.ChatId.Identifier == 123 && req.Text.Contains("нет питомцев")),
      Arg.Any<CancellationToken>());
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
      new PetDto(Guid.NewGuid(), familyId, "Fluffy", PetType.Cat, 85),
      new PetDto(Guid.NewGuid(), familyId, "Rex", PetType.Dog, 60)
    };

    _mediator.Send(Arg.Any<GetPetsQuery>(), Arg.Any<CancellationToken>())
      .Returns(Result<List<PetDto>>.Success(pets));

    // Act
    await _handler.HandleAsync(_botClient, message, session, userId, CancellationToken.None);

    // Assert
    await _botClient.Received(1).MakeRequestAsync(
      Arg.Is<SendMessageRequest>(req => 
        req.Text.Contains("Fluffy") && 
        req.Text.Contains("Rex") &&
        req.Text.Contains("🐱") &&
        req.Text.Contains("🐶")),
      Arg.Any<CancellationToken>());
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
      new PetDto(Guid.NewGuid(), familyId, "Happy Cat", PetType.Cat, 90) // High mood
    };

    _mediator.Send(Arg.Any<GetPetsQuery>(), Arg.Any<CancellationToken>())
      .Returns(Result<List<PetDto>>.Success(pets));

    // Act
    await _handler.HandleAsync(_botClient, message, session, userId, CancellationToken.None);

    // Assert
    await _botClient.Received(1).MakeRequestAsync(
      Arg.Is<SendMessageRequest>(req => req.Text.Contains("😊") && req.Text.Contains("Отлично")),
      Arg.Any<CancellationToken>());
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
      new PetDto(Guid.NewGuid(), familyId, "Sad Cat", PetType.Cat, 15) // Low mood
    };

    _mediator.Send(Arg.Any<GetPetsQuery>(), Arg.Any<CancellationToken>())
      .Returns(Result<List<PetDto>>.Success(pets));

    // Act
    await _handler.HandleAsync(_botClient, message, session, userId, CancellationToken.None);

    // Assert
    await _botClient.Received(1).MakeRequestAsync(
      Arg.Is<SendMessageRequest>(req => req.Text.Contains("😢") && req.Text.Contains("Очень грустно")),
      Arg.Any<CancellationToken>());
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
      new PetDto(Guid.NewGuid(), familyId, "Cat", PetType.Cat, 50),
      new PetDto(Guid.NewGuid(), familyId, "Dog", PetType.Dog, 50),
      new PetDto(Guid.NewGuid(), familyId, "Hamster", PetType.Hamster, 50)
    };

    _mediator.Send(Arg.Any<GetPetsQuery>(), Arg.Any<CancellationToken>())
      .Returns(Result<List<PetDto>>.Success(pets));

    // Act
    await _handler.HandleAsync(_botClient, message, session, userId, CancellationToken.None);

    // Assert
    await _botClient.Received(1).MakeRequestAsync(
      Arg.Is<SendMessageRequest>(req => 
        req.Text.Contains("Кот") && 
        req.Text.Contains("Собака") && 
        req.Text.Contains("Хомяк")),
      Arg.Any<CancellationToken>());
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
    await _botClient.Received(1).MakeRequestAsync(
      Arg.Is<SendMessageRequest>(req => req.ChatId.Identifier == 123 && req.Text.Contains("Ошибка")),
      Arg.Any<CancellationToken>());
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
