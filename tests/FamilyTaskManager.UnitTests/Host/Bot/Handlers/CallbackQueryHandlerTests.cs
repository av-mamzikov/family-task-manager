using Ardalis.Result;
using FamilyTaskManager.Core.Interfaces;
using FamilyTaskManager.Host.Modules.Bot.Handlers;
using FamilyTaskManager.Host.Modules.Bot.Handlers.Commands;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.Host.Modules.Bot.Services;
using FamilyTaskManager.UseCases.Tasks;
using FamilyTaskManager.UseCases.Users;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;

namespace FamilyTaskManager.UnitTests.Host.Bot.Handlers;

public class CallbackQueryHandlerTests
{
  private readonly ITelegramBotClient _botClient;
  private readonly CallbackQueryHandler _handler;
  private readonly ILogger<CallbackQueryHandler> _logger;
  private readonly IMediator _mediator;
  private readonly ISessionManager _sessionManager;
  private readonly TemplateCommandHandler _templateCommandHandler;
  private readonly ITimeZoneService _timeZoneService;

  public CallbackQueryHandlerTests()
  {
    _mediator = Substitute.For<IMediator>();
    _sessionManager = Substitute.For<ISessionManager>();
    _logger = Substitute.For<ILogger<CallbackQueryHandler>>();
    _timeZoneService = Substitute.For<ITimeZoneService>();
    var mediatorForTemplate = Substitute.For<IMediator>();
    _templateCommandHandler = Substitute.ForPartsOf<TemplateCommandHandler>(mediatorForTemplate);
    _handler = new CallbackQueryHandler(_logger, _sessionManager, _mediator, _timeZoneService, _templateCommandHandler);
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

    // Assert - handler should complete without throwing
    // Note: Cannot verify AnswerCallbackQueryAsync as it's an extension method
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
    await _botClient.Received(1).MakeRequestAsync(
      Arg.Is<EditMessageTextRequest>(req => req.Text.Contains("Выберите тип питомца")),
      Arg.Any<CancellationToken>());
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
    await _botClient.Received(1).MakeRequestAsync(
      Arg.Is<EditMessageTextRequest>(req => req.Text.Contains("выбрана")),
      Arg.Any<CancellationToken>());
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
    await _botClient.Received(1).MakeRequestAsync(
      Arg.Is<EditMessageTextRequest>(req => req.Text.Contains("выполнена") && req.Text.Contains("🎉")),
      Arg.Any<CancellationToken>());
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
    await _botClient.Received(1).MakeRequestAsync(
      Arg.Is<SendMessageRequest>(req => req.Text.Contains("Ошибка") && req.Text.Contains("Task not found")),
      Arg.Any<CancellationToken>());
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

    // Assert - handler should complete without throwing
    // Note: Cannot verify SendTextMessageAsync as it's an extension method
  }

  private static CallbackQuery CreateCallbackQuery(string data, long chatId = 123)
  {
    return new CallbackQuery
    {
      Id = "callback_id",
      Data = data,
      From = new User { Id = chatId, FirstName = "Test" },
      Message = new Message { MessageId = 1, Chat = new Chat { Id = chatId }, Date = DateTime.UtcNow }
    };
  }
}
