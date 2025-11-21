using FamilyTaskManager.Host.Modules.Bot.Handlers;
using FamilyTaskManager.Host.Modules.Bot.Services;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.UseCases.Users;
using FamilyTaskManager.UseCases.Families;
using FamilyTaskManager.UseCases.Pets;
using FamilyTaskManager.UseCases.Tasks;
using FamilyTaskManager.Core.PetAggregate;
using Mediator;
using Telegram.Bot;
using Telegram.Bot.Types;
using Ardalis.Result;

namespace FamilyTaskManager.UnitTests.Host.Bot.Handlers;

public class CallbackQueryHandlerTests
{
  private readonly IMediator _mediator;
  private readonly ISessionManager _sessionManager;
  private readonly ILogger<CallbackQueryHandler> _logger;
  private readonly CallbackQueryHandler _handler;
  private readonly ITelegramBotClient _botClient;

  public CallbackQueryHandlerTests()
  {
    _mediator = Substitute.For<IMediator>();
    _sessionManager = Substitute.For<ISessionManager>();
    _logger = Substitute.For<ILogger<CallbackQueryHandler>>();
    _handler = new CallbackQueryHandler(_logger, _sessionManager, _mediator);
    _botClient = Substitute.For<ITelegramBotClient>();
  }

  [Fact]
  public async Task HandleCallbackAsync_ShouldAnswerCallbackQuery()
  {
    // Arrange
    var callbackQuery = CreateCallbackQuery("create_family");
    var session = new UserSession();
    _sessionManager.GetSession(Arg.Any<long>()).Returns(session);

    // Act
    await _handler.HandleCallbackAsync(_botClient, callbackQuery, CancellationToken.None);

    // Assert
    await _botClient.Received(1).AnswerCallbackQueryAsync(
      Arg.Is<string>(callbackQuery.Id),
      cancellationToken: Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task HandleCallbackAsync_ShouldStartCreateFamily_WhenCreateFamilyClicked()
  {
    // Arrange
    var callbackQuery = CreateCallbackQuery("create_family");
    var session = new UserSession();
    var userId = Guid.NewGuid();

    _sessionManager.GetSession(Arg.Any<long>()).Returns(session);
    _mediator.Send(Arg.Any<RegisterUserCommand>(), Arg.Any<CancellationToken>())
      .Returns(Result<Guid>.Success(userId));

    // Act
    await _handler.HandleCallbackAsync(_botClient, callbackQuery, CancellationToken.None);

    // Assert
    session.State.ShouldBe(ConversationState.AwaitingFamilyName);
    session.Data.ShouldContainKey("userId");
    session.Data["userId"].ShouldBe(userId);
  }

  [Fact]
  public async Task HandleCallbackAsync_ShouldShowPetTypeSelection_WhenCreatePetClicked()
  {
    // Arrange
    var callbackQuery = CreateCallbackQuery("create_pet");
    var familyId = Guid.NewGuid();
    var session = new UserSession { CurrentFamilyId = familyId };

    _sessionManager.GetSession(Arg.Any<long>()).Returns(session);

    // Act
    await _handler.HandleCallbackAsync(_botClient, callbackQuery, CancellationToken.None);

    // Assert
    await _botClient.Received(1).EditMessageTextAsync(
      Arg.Any<long>(),
      Arg.Any<int>(),
      Arg.Is<string>(text => text.Contains("Выберите тип питомца")),
      replyMarkup: Arg.Any<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup?>(),
      cancellationToken: Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task HandleCallbackAsync_ShouldSetPetTypeState_WhenPetTypeSelected()
  {
    // Arrange
    var callbackQuery = CreateCallbackQuery("select_pettype_cat");
    var familyId = Guid.NewGuid();
    var session = new UserSession { CurrentFamilyId = familyId };

    _sessionManager.GetSession(Arg.Any<long>()).Returns(session);

    // Act
    await _handler.HandleCallbackAsync(_botClient, callbackQuery, CancellationToken.None);

    // Assert
    session.State.ShouldBe(ConversationState.AwaitingPetName);
    session.Data.ShouldContainKey("petType");
    session.Data["petType"].ShouldBe("cat");
    session.Data.ShouldContainKey("familyId");
  }

  [Fact]
  public async Task HandleCallbackAsync_ShouldSwitchFamily_WhenFamilySelected()
  {
    // Arrange
    var familyId = Guid.NewGuid();
    var callbackQuery = CreateCallbackQuery($"select_family_{familyId}");
    var session = new UserSession();

    _sessionManager.GetSession(Arg.Any<long>()).Returns(session);

    // Act
    await _handler.HandleCallbackAsync(_botClient, callbackQuery, CancellationToken.None);

    // Assert
    session.CurrentFamilyId.ShouldBe(familyId);
    await _botClient.Received(1).EditMessageTextAsync(
      Arg.Any<long>(),
      Arg.Any<int>(),
      Arg.Is<string>(text => text.Contains("выбрана")),
      cancellationToken: Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task HandleCallbackAsync_ShouldTakeTask_WhenTaskTakeClicked()
  {
    // Arrange
    var taskId = Guid.NewGuid();
    var userId = Guid.NewGuid();
    var callbackQuery = CreateCallbackQuery($"task_take_{taskId}");
    var session = new UserSession();

    _sessionManager.GetSession(Arg.Any<long>()).Returns(session);
    _mediator.Send(Arg.Any<RegisterUserCommand>(), Arg.Any<CancellationToken>())
      .Returns(Result<Guid>.Success(userId));
    _mediator.Send(Arg.Any<TakeTaskCommand>(), Arg.Any<CancellationToken>())
      .Returns(Result.Success());

    // Act
    await _handler.HandleCallbackAsync(_botClient, callbackQuery, CancellationToken.None);

    // Assert
    await _mediator.Received(1).Send(
      Arg.Is<TakeTaskCommand>(cmd => cmd.TaskId == taskId && cmd.UserId == userId),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task HandleCallbackAsync_ShouldCompleteTask_WhenTaskCompleteClicked()
  {
    // Arrange
    var taskId = Guid.NewGuid();
    var userId = Guid.NewGuid();
    var callbackQuery = CreateCallbackQuery($"task_complete_{taskId}");
    var session = new UserSession();

    _sessionManager.GetSession(Arg.Any<long>()).Returns(session);
    _mediator.Send(Arg.Any<RegisterUserCommand>(), Arg.Any<CancellationToken>())
      .Returns(Result<Guid>.Success(userId));
    _mediator.Send(Arg.Any<CompleteTaskCommand>(), Arg.Any<CancellationToken>())
      .Returns(Result.Success());

    // Act
    await _handler.HandleCallbackAsync(_botClient, callbackQuery, CancellationToken.None);

    // Assert
    await _mediator.Received(1).Send(
      Arg.Is<CompleteTaskCommand>(cmd => cmd.TaskId == taskId && cmd.UserId == userId),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task HandleCallbackAsync_ShouldShowSuccessMessage_WhenTaskCompleted()
  {
    // Arrange
    var taskId = Guid.NewGuid();
    var userId = Guid.NewGuid();
    var callbackQuery = CreateCallbackQuery($"task_complete_{taskId}");
    var session = new UserSession();

    _sessionManager.GetSession(Arg.Any<long>()).Returns(session);
    _mediator.Send(Arg.Any<RegisterUserCommand>(), Arg.Any<CancellationToken>())
      .Returns(Result<Guid>.Success(userId));
    _mediator.Send(Arg.Any<CompleteTaskCommand>(), Arg.Any<CancellationToken>())
      .Returns(Result.Success());

    // Act
    await _handler.HandleCallbackAsync(_botClient, callbackQuery, CancellationToken.None);

    // Assert
    await _botClient.Received(1).EditMessageTextAsync(
      Arg.Any<long>(),
      Arg.Any<int>(),
      Arg.Is<string>(text => text.Contains("выполнена") && text.Contains("🎉")),
      cancellationToken: Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task HandleCallbackAsync_ShouldShowError_WhenTaskCompletionFails()
  {
    // Arrange
    var taskId = Guid.NewGuid();
    var userId = Guid.NewGuid();
    var callbackQuery = CreateCallbackQuery($"task_complete_{taskId}");
    var session = new UserSession();

    _sessionManager.GetSession(Arg.Any<long>()).Returns(session);
    _mediator.Send(Arg.Any<RegisterUserCommand>(), Arg.Any<CancellationToken>())
      .Returns(Result<Guid>.Success(userId));
    _mediator.Send(Arg.Any<CompleteTaskCommand>(), Arg.Any<CancellationToken>())
      .Returns(Result.Error("Task not found"));

    // Act
    await _handler.HandleCallbackAsync(_botClient, callbackQuery, CancellationToken.None);

    // Assert
    await _botClient.Received(1).SendTextMessageAsync(
      Arg.Any<long>(),
      Arg.Is<string>(text => text.Contains("Ошибка") && text.Contains("Task not found")),
      cancellationToken: Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task HandleCallbackAsync_ShouldUpdateActivity_WhenHandlingCallback()
  {
    // Arrange
    var callbackQuery = CreateCallbackQuery("create_family");
    var session = Substitute.For<UserSession>();
    _sessionManager.GetSession(Arg.Any<long>()).Returns(session);

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
    _sessionManager.GetSession(Arg.Any<long>()).Returns(session);

    // Act
    await _handler.HandleCallbackAsync(_botClient, callbackQuery, CancellationToken.None);

    // Assert
    await _botClient.Received(1).SendTextMessageAsync(
      Arg.Any<long>(),
      Arg.Is<string>(text => text.Contains("Неизвестное действие")),
      cancellationToken: Arg.Any<CancellationToken>());
  }

  private static CallbackQuery CreateCallbackQuery(string data, long chatId = 123)
  {
    return new CallbackQuery
    {
      Id = "callback_id",
      Data = data,
      From = new User { Id = chatId, FirstName = "Test" },
      Message = new Message
      {
        MessageId = 1,
        Chat = new Chat { Id = chatId },
        Date = DateTime.UtcNow
      }
    };
  }
}
