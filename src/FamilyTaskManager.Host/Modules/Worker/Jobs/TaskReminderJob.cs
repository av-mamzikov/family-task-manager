using Mediator;
using Quartz;
using FamilyTaskManager.UseCases.Tasks;

namespace FamilyTaskManager.Host.Modules.Worker.Jobs;

/// <summary>
/// Job that triggers reminders for tasks due within the next hour.
/// Runs every 15 minutes.
/// Domain events handle the actual notification sending.
/// </summary>
[DisallowConcurrentExecution]
public class TaskReminderJob : IJob
{
  private readonly IMediator _mediator;
  private readonly ILogger<TaskReminderJob> _logger;

  public TaskReminderJob(
    IMediator mediator,
    ILogger<TaskReminderJob> logger)
  {
    _mediator = mediator;
    _logger = logger;
  }

  public async Task Execute(IJobExecutionContext context)
  {
    _logger.LogInformation("TaskReminderJob started at {Time}", DateTime.UtcNow);

    try
    {
      // Simply call the UseCase - it handles all the logic
      var result = await _mediator.Send(new SendTaskRemindersCommand(), context.CancellationToken);

      if (result.IsSuccess)
      {
        _logger.LogInformation("TaskReminderJob completed successfully");
      }
      else
      {
        _logger.LogWarning("TaskReminderJob completed with errors: {Errors}", result.Errors);
      }
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "TaskReminderJob failed");
      throw;
    }
  }
}
