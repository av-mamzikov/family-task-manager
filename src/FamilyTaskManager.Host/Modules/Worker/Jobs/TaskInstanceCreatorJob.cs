using Mediator;
using Quartz;
using FamilyTaskManager.UseCases.Tasks;

namespace FamilyTaskManager.Host.Modules.Worker.Jobs;

/// <summary>
/// Job that creates TaskInstance from TaskTemplate based on schedule.
/// Runs every minute to check for templates that need new instances.
/// </summary>
[DisallowConcurrentExecution]
public class TaskInstanceCreatorJob : IJob
{
  private readonly IMediator _mediator;
  private readonly ILogger<TaskInstanceCreatorJob> _logger;

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
      // Get all active task templates
      var templatesResult = await _mediator.Send(new GetActiveTaskTemplatesQuery());
      
      if (!templatesResult.IsSuccess)
      {
        _logger.LogWarning("Failed to get active task templates: {Error}", templatesResult.Errors);
        return;
      }

      var templates = templatesResult.Value;
      _logger.LogInformation("Found {Count} active task templates", templates.Count);

      var now = DateTime.UtcNow;
      var createdCount = 0;

      foreach (var template in templates)
      {
        try
        {
          // Parse Quartz Cron expression
          var cronExpression = new CronExpression(template.Schedule);
          
          // Get the next occurrence after the last check (we check every minute, so look back 2 minutes to be safe)
          var checkFrom = now.AddMinutes(-2);
          var nextOccurrence = cronExpression.GetTimeAfter(DateTimeOffset.FromUnixTimeSeconds(checkFrom.Ticks / TimeSpan.TicksPerSecond));

          // If next occurrence is in the past minute, create a new instance
          if (nextOccurrence.HasValue && nextOccurrence.Value.UtcDateTime <= now && nextOccurrence.Value.UtcDateTime > checkFrom)
          {
            _logger.LogInformation(
              "Creating TaskInstance for template {TemplateId} ({Title}), due at {DueAt}",
              template.Id, template.Title, nextOccurrence.Value);

            var createResult = await _mediator.Send(
              new CreateTaskInstanceFromTemplateCommand(template.Id, nextOccurrence.Value.UtcDateTime));

            if (createResult.IsSuccess)
            {
              createdCount++;
              _logger.LogInformation(
                "Successfully created TaskInstance {InstanceId} from template {TemplateId}",
                createResult.Value, template.Id);
            }
            else
            {
              // This is expected if there's already an active instance
              _logger.LogDebug(
                "Could not create TaskInstance for template {TemplateId}: {Error}",
                template.Id, string.Join(", ", createResult.Errors));
            }
          }
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, 
            "Error processing template {TemplateId} with schedule {Schedule}", 
            template.Id, template.Schedule);
        }
      }

      _logger.LogInformation(
        "TaskInstanceCreatorJob completed. Created {CreatedCount} new task instances", 
        createdCount);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error in TaskInstanceCreatorJob");
    }
  }
}
