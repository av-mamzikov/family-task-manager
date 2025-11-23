using FamilyTaskManager.UseCases.Tasks;
using Quartz;

namespace FamilyTaskManager.Host.Modules.Worker.Jobs;

/// <summary>
///   Job that creates TaskInstance from TaskTemplate based on schedule.
///   Runs every minute to check for templates that need new instances.
///   This is a thin orchestrator that delegates business logic to use cases.
/// </summary>
[DisallowConcurrentExecution]
public class TaskInstanceCreatorJob : IJob
{
  private readonly ILogger<TaskInstanceCreatorJob> _logger;
  private readonly IMediator _mediator;

  public TaskInstanceCreatorJob(IMediator mediator, ILogger<TaskInstanceCreatorJob> logger)
  {
    _mediator = mediator;
    _logger = logger;
  }

  public async Task Execute(IJobExecutionContext context)
  {
    _logger.LogInformation("TaskInstanceCreatorJob started at {Time}", DateTimeOffset.UtcNow);

    try
    {
      var now = DateTime.UtcNow;

      // Check the last 2 minutes to be safe (job runs every minute)
      var checkFrom = now.AddMinutes(-2);
      var checkTo = now;

      var result = await _mediator.Send(
        new ProcessScheduledTaskCommand(checkFrom, checkTo),
        context.CancellationToken);

      if (result.IsSuccess)
      {
        _logger.LogInformation(
          "TaskInstanceCreatorJob completed. Created {CreatedCount} new task instances",
          result.Value);
      }
      else
      {
        _logger.LogWarning("Failed to process scheduled tasks: {Error}",
          string.Join(", ", result.Errors));
      }
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error in TaskInstanceCreatorJob");
    }
  }
}
