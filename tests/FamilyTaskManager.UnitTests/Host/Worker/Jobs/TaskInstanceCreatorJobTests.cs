using Ardalis.Result;
using FamilyTaskManager.Core.TaskAggregate;
using FamilyTaskManager.UseCases.Tasks;
using FamilyTaskManager.Host.Modules.Worker.Jobs;
using Mediator;
using Microsoft.Extensions.Logging;
using Quartz;

namespace FamilyTaskManager.UnitTests.Host.Worker.Jobs;

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
        _mediator.Send(Arg.Any<ProcessScheduledTaskCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(1));

        // Act
        await _job.Execute(_context);

        // Assert
        await _mediator.Received(1).Send(
            Arg.Any<ProcessScheduledTaskCommand>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_DoesNotThrow_WhenNoTemplatesExist()
    {
        // Arrange
        _mediator.Send(Arg.Any<ProcessScheduledTaskCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(0));

        // Act & Assert
        await Should.NotThrowAsync(async () => await _job.Execute(_context));
    }

    [Fact]
    public async Task Execute_HandlesErrors_Gracefully()
    {
        // Arrange
        _mediator.Send(Arg.Any<ProcessScheduledTaskCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Error("Database error"));

        // Act & Assert
        await Should.NotThrowAsync(async () => await _job.Execute(_context));
    }

    [Fact]
    public async Task Execute_DoesNotCreateInstance_WhenActiveInstanceExists()
    {
        // Arrange
        _mediator.Send(Arg.Any<ProcessScheduledTaskCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(0));

        // Act
        await _job.Execute(_context);

        // Assert - should not throw, just log
        await _mediator.Received(1).Send(
            Arg.Any<ProcessScheduledTaskCommand>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_ProcessesMultipleTemplates()
    {
        // Arrange
        _mediator.Send(Arg.Any<ProcessScheduledTaskCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(3));

        // Act
        await _job.Execute(_context);

        // Assert
        await _mediator.Received(1).Send(
            Arg.Any<ProcessScheduledTaskCommand>(),
            Arg.Any<CancellationToken>());
    }
}
