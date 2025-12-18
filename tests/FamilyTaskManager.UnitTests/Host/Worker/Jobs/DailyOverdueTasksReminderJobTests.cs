using Ardalis.Result;
using FamilyTaskManager.Host.Modules.Worker.Jobs;
using FamilyTaskManager.UseCases.Features.TasksManagement.Commands;
using Quartz;

namespace FamilyTaskManager.UnitTests.Host.Worker.Jobs;

public class DailyOverdueTasksReminderJobTests
{
  private readonly IJobExecutionContext _context;
  private readonly DailyOverdueTasksReminderJob _job;
  private readonly ILogger<DailyOverdueTasksReminderJob> _logger;
  private readonly IMediator _mediator;

  public DailyOverdueTasksReminderJobTests()
  {
    _mediator = Substitute.For<IMediator>();
    _logger = Substitute.For<ILogger<DailyOverdueTasksReminderJob>>();
    _job = new(_mediator, _logger);
    _context = Substitute.For<IJobExecutionContext>();
  }

  [Fact]
  public async Task Execute_SendsCommand_WithFireTimes()
  {
    // Arrange
    var prev = new DateTimeOffset(new(2025, 12, 18, 13, 0, 0, DateTimeKind.Utc));
    var current = new DateTimeOffset(new(2025, 12, 18, 13, 15, 0, DateTimeKind.Utc));

    _context.PreviousFireTimeUtc.Returns(prev);
    _context.FireTimeUtc.Returns(current);

    _mediator.Send(Arg.Any<SendDailyOverdueTasksRemindersCommand>(), Arg.Any<CancellationToken>())
      .Returns(Result.Success());

    // Act
    await _job.Execute(_context);

    // Assert
    await _mediator.Received(1).Send(
      Arg.Is<SendDailyOverdueTasksRemindersCommand>(c =>
        c.PreviousFireTimeUtc == prev.UtcDateTime && c.CurrentFireTimeUtc == current.UtcDateTime),
      Arg.Any<CancellationToken>());
  }
}
