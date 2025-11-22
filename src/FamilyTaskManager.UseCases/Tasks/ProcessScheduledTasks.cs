using FamilyTaskManager.Core.Interfaces;
using Mediator;
using Microsoft.Extensions.Logging;

namespace FamilyTaskManager.UseCases.Tasks;

/// <summary>
/// Command to process scheduled task templates and create task instances for due schedules.
/// This use case orchestrates the workflow of getting active templates, evaluating their schedules,
/// and creating instances when needed.
/// </summary>
public record ProcessScheduledTaskCommand(DateTime CheckFrom, DateTime CheckTo) : ICommand<Result<int>>;

/// <summary>
/// Handler for ProcessScheduledTasksCommand.
/// Evaluates all active task templates and creates task instances for schedules that trigger
/// within the specified time window.
/// </summary>
public class ProcessScheduledTasksHandler(
    IRepository<TaskTemplate> templateRepository,
    IRepository<Family> familyRepository,
    IMediator mediator,
    IScheduleEvaluator scheduleEvaluator,
    ILogger<ProcessScheduledTasksHandler> logger) 
    : ICommandHandler<ProcessScheduledTaskCommand, Result<int>>
{
    public async ValueTask<Result<int>> Handle(ProcessScheduledTaskCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Get all active task templates
            var spec = new ActiveTaskTemplatesSpec();
            var templates = await templateRepository.ListAsync(spec, cancellationToken);
            
            logger.LogInformation("Found {Count} active task templates to evaluate", templates.Count);

            var createdCount = 0;

            foreach (var template in templates)
            {
                try
                {
                    // Get the family to access timezone
                    var family = await familyRepository.GetByIdAsync(template.FamilyId, cancellationToken);
                    if (family == null)
                    {
                        logger.LogWarning("Family {FamilyId} not found for template {TemplateId}", template.FamilyId, template.Id);
                        continue;
                    }

                    // Check if the template should trigger within our time window using family timezone
                    var (shouldTrigger, triggerTime) = scheduleEvaluator.ShouldTriggerInWindow(
                        template.Schedule, request.CheckFrom, request.CheckTo, family.Timezone);

                    if (shouldTrigger && triggerTime.HasValue)
                    {
                        logger.LogInformation(
                            "Creating TaskInstance for template {TemplateId} ({Title}), due at {DueAt} (family timezone: {Timezone})",
                            template.Id, template.Title, triggerTime.Value, family.Timezone);

                        // Delegate task instance creation to existing command
                        var createResult = await mediator.Send(
                            new CreateTaskInstanceFromTemplateCommand(template.Id, triggerTime.Value),
                            cancellationToken);

                        if (createResult.IsSuccess)
                        {
                            createdCount++;
                            logger.LogInformation(
                                "Successfully created TaskInstance {InstanceId} from template {TemplateId}",
                                createResult.Value, template.Id);
                        }
                        else
                        {
                            // This is expected if there's already an active instance
                            logger.LogDebug(
                                "Could not create TaskInstance for template {TemplateId}: {Error}",
                                template.Id, string.Join(", ", createResult.Errors));
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, 
                        "Error processing template {TemplateId} with schedule {Schedule}", 
                        template.Id, template.Schedule);
                }
            }
            
            logger.LogInformation(
                "ProcessScheduledTasks completed. Created {CreatedCount} new task instances", 
                createdCount);

            return Result.Success(createdCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in ProcessScheduledTasksHandler");
            return Result.Error($"Failed to process scheduled tasks: {ex.Message}");
        }
    }
}
