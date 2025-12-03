using FamilyTaskManager.Core.Services;
using FamilyTaskManager.UseCases.TaskTemplates.Specifications;
using Microsoft.Extensions.Logging;

namespace FamilyTaskManager.UseCases.Tasks;

/// <summary>
///   Command to process scheduled task templates and create task instances for due schedules.
///   This use case orchestrates the workflow of getting active templates, evaluating their schedules,
///   and creating instances when needed.
/// </summary>
public record ProcessScheduledTaskCommand(DateTime CheckFrom, DateTime CheckTo) : ICommand<Result<int>>;

/// <summary>
///   Handler for ProcessScheduledTasksCommand.
///   Evaluates all active task templates and creates task instances for schedules that trigger
///   within the specified time window.
/// </summary>
public class ProcessScheduledTasksHandler(
  IReadRepository<TaskTemplate> templateRepository,
  IAppRepository<TaskInstance> taskAppRepository,
  IAppRepository<Pet> petAppRepository,
  ITaskInstanceFactory taskInstanceFactory,
  ILogger<ProcessScheduledTasksHandler> logger)
  : ICommandHandler<ProcessScheduledTaskCommand, Result<int>>
{
  public async ValueTask<Result<int>> Handle(ProcessScheduledTaskCommand request, CancellationToken cancellationToken)
  {
    try
    {
      var spec = new ActiveTaskTemplatesWithTimeZoneSpec();
      var templateDtos = await templateRepository.ListAsync(spec, cancellationToken);

      logger.LogInformation("Found {Count} active task templates to evaluate", templateDtos.Count);

      // Load full TaskTemplate entities for the domain service
      var templateIds = templateDtos.Select(t => t.Id).ToList();
      var templates =
        await templateRepository.ListAsync(new TaskTemplatesWithFamilyByIdsSpec(templateIds), cancellationToken);

      var createdCount = 0;

      foreach (var template in templates)
      {
        try
        {
          var timeZone = TimeZoneInfo.FindSystemTimeZoneById(template.Family.Timezone);
          var triggerTime = template.Schedule.ShouldTriggerInWindow(
            request.CheckFrom, request.CheckTo, timeZone);
          if (!triggerTime.HasValue)
            continue;

          // Create task instance using domain service
          // Calculate dueAt as scheduled time + due duration
          var dueAt = triggerTime.Value + template.DueDuration;

          logger.LogInformation(
            "Creating TaskInstance for template {TemplateId} ({Title}), scheduled at {ScheduledTime}, due at {DueAt} (family timezone: {Timezone})",
            template.Id, template.Title, triggerTime.Value, dueAt, template.Family.Timezone);

          // Load pet with family (needed for TaskCreatedEvent)
          var petSpec = new GetPetByIdWithFamilySpec(template.PetId);
          var pet = await petAppRepository.FirstOrDefaultAsync(petSpec, cancellationToken);
          if (pet == null)
          {
            logger.LogWarning("Pet {PetId} not found for template {TemplateId}, skipping", template.PetId, template.Id);
            continue;
          }

          // Get existing instances for this template
          var existingSpec = new TaskInstancesByTemplateSpec(template.Id);
          var existingInstances = await taskAppRepository.ListAsync(existingSpec, cancellationToken);
          var createResult = taskInstanceFactory.CreateFromTemplate(template, pet, dueAt, existingInstances);

          if (createResult.IsSuccess)
          {
            await taskAppRepository.AddAsync(createResult.Value, cancellationToken);
            await taskAppRepository.SaveChangesAsync(cancellationToken);

            createdCount++;
            logger.LogInformation(
              "Successfully created TaskInstance {InstanceId} from template {TemplateId}",
              createResult.Value.Id, template.Id);
          }
          else
          {
            // This is expected if there's already an active instance
            logger.LogDebug(
              "Could not create TaskInstance for template {TemplateId}: {Error} because it is already active",
              template.Id, string.Join(", ", createResult.Errors));
          }
        }
        catch (Exception ex)
        {
          logger.LogError(ex, "Error processing template {TemplateId} with schedule {Schedule}", template.Id,
            template.Schedule);
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
