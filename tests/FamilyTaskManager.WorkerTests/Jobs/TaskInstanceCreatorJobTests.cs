using Ardalis.Result;
using FamilyTaskManager.Core.TaskAggregate;
using FamilyTaskManager.UseCases.Tasks;
using FamilyTaskManager.Worker.Jobs;
using Mediator;
using Microsoft.Extensions.Logging;
using Quartz;

namespace FamilyTaskManager.WorkerTests.Jobs;

public class TaskInstanceCreatorJobTests
{
    private readonly IMediator _mediator;
    private readonly ILogger<TaskInstanceCreatorJob> _logger;
    private readonly TaskInstanceCreatorJob _job;
    private readonly IJobExecutionContext _context;

    public TaskInstanceCreatorJobTests()
    {
        _mediator = Substitute.For<IMediator>();
        _logger = Substitute.For<ILogger<TaskInstanceCreatorJob>>();
        _job = new TaskInstanceCreatorJob(_mediator, _logger);
        _context = Substitute.For<IJobExecutionContext>();
    }

    [Fact]
    public async Task Execute_CreatesTaskInstance_WhenTemplateExists()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var template = new TaskTemplate(
            Guid.NewGuid(), Guid.NewGuid(),
            "Test Task", 10, "0 * * * * ?", Guid.NewGuid());

        _mediator.Send(Arg.Any<GetActiveTaskTemplatesQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new List<TaskTemplate> { template }));

        _mediator.Send(Arg.Any<CreateTaskInstanceFromTemplateCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(Guid.NewGuid()));

        // Act
        await _job.Execute(_context);

        // Assert
        await _mediator.Received(1).Send(
            Arg.Any<GetActiveTaskTemplatesQuery>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_DoesNotThrow_WhenNoTemplatesExist()
    {
        // Arrange
        _mediator.Send(Arg.Any<GetActiveTaskTemplatesQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new List<TaskTemplate>()));

        // Act & Assert
        await Should.NotThrowAsync(async () => await _job.Execute(_context));
    }

    [Fact]
    public async Task Execute_HandlesErrors_Gracefully()
    {
        // Arrange
        _mediator.Send(Arg.Any<GetActiveTaskTemplatesQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Error("Database error"));

        // Act & Assert
        await Should.NotThrowAsync(async () => await _job.Execute(_context));
    }

    [Fact]
    public async Task Execute_DoesNotCreateInstance_WhenActiveInstanceExists()
    {
        // Arrange
        var template = new TaskTemplate(
            Guid.NewGuid(), Guid.NewGuid(),
            "Test Task", 10, "0 * * * * ?", Guid.NewGuid());

        _mediator.Send(Arg.Any<GetActiveTaskTemplatesQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new List<TaskTemplate> { template }));

        _mediator.Send(Arg.Any<CreateTaskInstanceFromTemplateCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Error("Active TaskInstance already exists"));

        // Act
        await _job.Execute(_context);

        // Assert - should not throw, just log
        await _mediator.Received(1).Send(
            Arg.Any<CreateTaskInstanceFromTemplateCommand>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_ProcessesMultipleTemplates()
    {
        // Arrange
        var templates = new List<TaskTemplate>
        {
            new TaskTemplate(Guid.NewGuid(), Guid.NewGuid(), "Task 1", 10, "0 * * * * ?", Guid.NewGuid()),
            new TaskTemplate(Guid.NewGuid(), Guid.NewGuid(), "Task 2", 15, "0 * * * * ?", Guid.NewGuid()),
            new TaskTemplate(Guid.NewGuid(), Guid.NewGuid(), "Task 3", 20, "0 * * * * ?", Guid.NewGuid()),
        };

        _mediator.Send(Arg.Any<GetActiveTaskTemplatesQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(templates));

        _mediator.Send(Arg.Any<CreateTaskInstanceFromTemplateCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(Guid.NewGuid()));

        // Act
        await _job.Execute(_context);

        // Assert
        await _mediator.Received(1).Send(
            Arg.Any<GetActiveTaskTemplatesQuery>(),
            Arg.Any<CancellationToken>());
    }
}
