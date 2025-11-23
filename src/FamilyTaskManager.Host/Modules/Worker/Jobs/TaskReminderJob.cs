using FamilyTaskManager.UseCases.Tasks;
using Quartz;

namespace FamilyTaskManager.Host.Modules.Worker.Jobs;

/// <summary>
///   Job that triggers reminders for tasks due within the next hour.
///   Runs every 15 minutes.
///   Domain events handle the actual notification sending.
/// </summary>
[DisallowConcurrentExecution]
public class TaskReminderJob(
  IMediator mediator,
  ILogger<TaskReminderJob> logger) : IJob
{
  private readonly IMediator _mediator = mediator;

  public async Task Execute(IJobExecutionContext context)
  {
    logger.LogInformation("TaskReminderJob started at {Time}", DateTime.UtcNow);

    try
    {
      // Simply call the UseCase - it handles all the logic
      var result = await _mediator.Send(new SendTaskRemindersCommand(), context.CancellationToken);

      if (result.IsSuccess)
      {
        logger.LogInformation("TaskReminderJob completed successfully");
      }
      else
      {
        logger.LogWarning("TaskReminderJob completed with errors: {Errors}", result.Errors);
      }
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "TaskReminderJob failed");
      throw;
    }
  }
}
