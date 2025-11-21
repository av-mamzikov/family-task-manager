using Mediator;
using Quartz;
using FamilyTaskManager.UseCases.Tasks;
using FamilyTaskManager.UseCases.Users;
using FamilyTaskManager.UseCases.Notifications;

namespace FamilyTaskManager.Host.Modules.Worker.Jobs;

/// <summary>
/// Job that sends reminders for tasks due within the next hour.
/// Runs every 15 minutes.
/// </summary>
[DisallowConcurrentExecution]
public class TaskReminderJob : IJob
{
  private readonly IMediator _mediator;
  private readonly IServiceProvider _serviceProvider;
  private readonly ILogger<TaskReminderJob> _logger;

  public TaskReminderJob(
    IMediator mediator, 
    IServiceProvider serviceProvider,
    ILogger<TaskReminderJob> logger)
  {
    _mediator = mediator;
    _serviceProvider = serviceProvider;
    _logger = logger;
  }

  public async Task Execute(IJobExecutionContext context)
  {
    _logger.LogInformation("TaskReminderJob started at {Time}", DateTimeOffset.UtcNow);

    try
    {
      var now = DateTime.UtcNow;
      var reminderWindow = now.AddHours(1); // Send reminders for tasks due in the next hour

      // Get tasks that need reminders
      var tasksResult = await _mediator.Send(
        new GetTasksDueForReminderQuery(now, reminderWindow));

      if (!tasksResult.IsSuccess)
      {
        _logger.LogWarning("Failed to get tasks for reminder: {Error}", tasksResult.Errors);
        return;
      }

      var tasks = tasksResult.Value;
      _logger.LogInformation("Found {Count} tasks requiring reminders", tasks.Count);

      var sentCount = 0;

      // Create a scope to resolve scoped services
      using (var scope = _serviceProvider.CreateScope())
      {
        var notificationService = scope.ServiceProvider.GetRequiredService<ITelegramNotificationService>();

        foreach (var task in tasks)
        {
          try
          {
            // Get telegram IDs for all family members
            foreach (var userId in task.FamilyMemberIds)
            {
              var userResult = await _mediator.Send(new GetUserByIdQuery(userId));
              
              if (!userResult.IsSuccess || userResult.Value == null)
              {
                _logger.LogWarning("User {UserId} not found for reminder", userId);
                continue;
              }

              var user = userResult.Value;

              // Send Telegram notification
              await notificationService.SendTaskReminderAsync(user.TelegramId, task, context.CancellationToken);

              sentCount++;
            }
          }
          catch (Exception ex)
          {
            _logger.LogError(ex, 
              "Error sending reminder for task {TaskId}", 
              task.TaskId);
          }
        }
      }

      _logger.LogInformation(
        "TaskReminderJob completed. Sent {SentCount} reminders", 
        sentCount);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error in TaskReminderJob");
    }
  }
}
