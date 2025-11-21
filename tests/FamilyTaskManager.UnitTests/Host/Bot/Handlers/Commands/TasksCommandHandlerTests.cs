using FamilyTaskManager.Host.Modules.Bot.Handlers.Commands;
using FamilyTaskManager.Host.Modules.Bot.Models;
using FamilyTaskManager.UseCases.Tasks;
using Mediator;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Requests;
using Ardalis.Result;
using TaskStatus = FamilyTaskManager.Core.TaskAggregate.TaskStatus;

namespace FamilyTaskManager.UnitTests.Host.Bot.Handlers.Commands;

public class TasksCommandHandlerTests
{
  private readonly IMediator _mediator;
  private readonly TasksCommandHandler _handler;
  private readonly ITelegramBotClient _botClient;

  public TasksCommandHandlerTests()
  {
    _mediator = Substitute.For<IMediator>();
    _handler = new TasksCommandHandler(_mediator);
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
  public async Task HandleAsync_ShouldDisplayNoTasksMessage_WhenNoActiveTasks()
  {
    // Arrange
    var message = CreateMessage(chatId: 123);
    var familyId = Guid.NewGuid();
    var session = new UserSession { CurrentFamilyId = familyId };
    var userId = Guid.NewGuid();

    _mediator.Send(Arg.Any<GetActiveTasksQuery>(), Arg.Any<CancellationToken>())
      .Returns(Result<List<TaskDto>>.Success(new List<TaskDto>()));

    // Act
    await _handler.HandleAsync(_botClient, message, session, userId, CancellationToken.None);

    // Assert
    await _botClient.Received(1).MakeRequestAsync(
      Arg.Is<SendMessageRequest>(req => req.ChatId.Identifier == 123 && req.Text.Contains("Активных задач пока нет")),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task HandleAsync_ShouldDisplayTasks_WhenTasksExist()
  {
    // Arrange
    var message = CreateMessage(chatId: 123);
    var familyId = Guid.NewGuid();
    var session = new UserSession { CurrentFamilyId = familyId };
    var userId = Guid.NewGuid();

    var tasks = new List<TaskDto>
    {
      new TaskDto(
        Guid.NewGuid(),
        "Feed the cat",
        10,
        TaskStatus.Active,
        DateTime.UtcNow.AddHours(2),
        Guid.NewGuid(),
        "Fluffy")
    };

    _mediator.Send(Arg.Any<GetActiveTasksQuery>(), Arg.Any<CancellationToken>())
      .Returns(Result<List<TaskDto>>.Success(tasks));

    // Act
    await _handler.HandleAsync(_botClient, message, session, userId, CancellationToken.None);

    // Assert
    await _botClient.Received(1).MakeRequestAsync(
      Arg.Is<SendMessageRequest>(req => req.Text.Contains("Feed the cat") && req.Text.Contains("Fluffy") && req.Text.Contains("10")),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task HandleAsync_ShouldShowOverdueMarker_ForOverdueTasks()
  {
    // Arrange
    var message = CreateMessage(chatId: 123);
    var familyId = Guid.NewGuid();
    var session = new UserSession { CurrentFamilyId = familyId };
    var userId = Guid.NewGuid();

    var tasks = new List<TaskDto>
    {
      new TaskDto(
        Guid.NewGuid(),
        "Overdue task",
        10,
        TaskStatus.Active,
        DateTime.UtcNow.AddHours(-2), // Overdue
        Guid.NewGuid(),
        "Pet")
    };

    _mediator.Send(Arg.Any<GetActiveTasksQuery>(), Arg.Any<CancellationToken>())
      .Returns(Result<List<TaskDto>>.Success(tasks));

    // Act
    await _handler.HandleAsync(_botClient, message, session, userId, CancellationToken.None);

    // Assert
    await _botClient.Received(1).MakeRequestAsync(
      Arg.Is<SendMessageRequest>(req => req.Text.Contains("⚠️")),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task HandleAsync_ShouldGroupTasksByStatus()
  {
    // Arrange
    var message = CreateMessage(chatId: 123);
    var familyId = Guid.NewGuid();
    var session = new UserSession { CurrentFamilyId = familyId };
    var userId = Guid.NewGuid();

    var tasks = new List<TaskDto>
    {
      new TaskDto(Guid.NewGuid(), "Active task", 10, TaskStatus.Active, DateTime.UtcNow.AddHours(2), Guid.NewGuid(), "Pet1"),
      new TaskDto(Guid.NewGuid(), "In progress task", 15, TaskStatus.InProgress, DateTime.UtcNow.AddHours(1), Guid.NewGuid(), "Pet2")
    };

    _mediator.Send(Arg.Any<GetActiveTasksQuery>(), Arg.Any<CancellationToken>())
      .Returns(Result<List<TaskDto>>.Success(tasks));

    // Act
    await _handler.HandleAsync(_botClient, message, session, userId, CancellationToken.None);

    // Assert
    await _botClient.Received(1).MakeRequestAsync(
      Arg.Is<SendMessageRequest>(req => req.Text.Contains("Доступные задачи") && req.Text.Contains("В работе")),
      Arg.Any<CancellationToken>());
  }

  private static Message CreateMessage(long chatId, string text = "/tasks")
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
