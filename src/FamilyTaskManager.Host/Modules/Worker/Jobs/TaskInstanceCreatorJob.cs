using FamilyTaskManager.UseCases.Tasks;
using Quartz;

namespace FamilyTaskManager.Host.Modules.Worker.Jobs;

/// <summary>
///   Job that creates TaskInstance from TaskTemplate based on schedule.
///   Runs every minute to check for templates that need new instances.
///   This is a thin orchestrator that delegates business logic to use cases.
/// </summary>
[DisallowConcurrentExecution]
public class TaskInstanceCreatorJob(IMediator mediator, ILogger<TaskInstanceCreatorJob> logger) : IJob
{
  public async Task Execute(IJobExecutionContext context)
  {
    logger.LogInformation("TaskInstanceCreatorJob started at {Time}", DateTimeOffset.UtcNow);

    try
    {
      var now = DateTime.UtcNow;

      var checkFrom = now.AddMinutes(-60);
      var checkTo = now;

      var result = await mediator.Send(
        new ProcessScheduledTaskCommand(checkFrom, checkTo),
        context.CancellationToken);

      if (result.IsSuccess)
      {
        logger.LogInformation(
          "TaskInstanceCreatorJob completed. Created {CreatedCount} new task instances",
          result.Value);
      }
      else
      {
        logger.LogWarning("Failed to process scheduled tasks: {Error}",
          string.Join(", ", result.Errors));
      }
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error in TaskInstanceCreatorJob");
    }
  }
}
