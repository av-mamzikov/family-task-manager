using FamilyTaskManager.UseCases.Features.TasksManagement.Commands;
using Quartz;

namespace FamilyTaskManager.Host.Modules.Worker.Jobs;

[DisallowConcurrentExecution]
public class DailyOverdueTasksReminderJob(
  IMediator mediator,
  ILogger<DailyOverdueTasksReminderJob> logger) : IJob
{
  public async Task Execute(IJobExecutionContext context)
  {
    logger.LogInformation("DailyOverdueTasksReminderJob started at {Time}", DateTime.UtcNow);

    try
    {
      var previousFireTimeUtc = context.PreviousFireTimeUtc?.UtcDateTime;
      var currentFireTimeUtc = context.FireTimeUtc.UtcDateTime;

      var result = await mediator.Send(
        new SendDailyOverdueTasksRemindersCommand(previousFireTimeUtc, currentFireTimeUtc),
        context.CancellationToken);

      if (result.IsSuccess)
        logger.LogInformation("DailyOverdueTasksReminderJob completed successfully");
      else
        logger.LogWarning("DailyOverdueTasksReminderJob completed with errors: {Errors}", result.Errors);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "DailyOverdueTasksReminderJob failed");
      throw;
    }
  }
}
