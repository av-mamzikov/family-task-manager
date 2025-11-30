using FamilyTaskManager.Core.Interfaces;
using FamilyTaskManager.Core.PetAggregate;
using FamilyTaskManager.Core.TaskAggregate.Events;
using FamilyTaskManager.Infrastructure.Notifications;
using FamilyTaskManager.UseCases.Pets.Specifications;
using FamilyTaskManager.UseCases.Tasks;
using Mediator;

namespace FamilyTaskManager.Infrastructure.DomainEvents;

public class TaskReminderDueEventHandler(
  ILogger<TaskReminderDueEventHandler> logger,
  ITelegramNotificationService notificationService,
  IRepository<Pet> petRepository,
  ITimeZoneService timeZoneService)
  : INotificationHandler<TaskReminderDueEvent>
{
  public async ValueTask Handle(TaskReminderDueEvent notification, CancellationToken cancellationToken)
  {
    var task = notification.Task;

    logger.LogInformation("Task reminder due: {TaskId} - {TaskTitle}",
      task.Id, task.Title);

    try
    {
      // Get pet information with family data for timezone conversion
      var spec = new GetPetWithFamilySpec(task.PetId);
      var pet = await petRepository.FirstOrDefaultAsync(spec, cancellationToken);
      if (pet == null)
      {
        logger.LogWarning("Pet {PetId} not found for task {TaskId} reminder",
          task.PetId, task.Id);
        return;
      }

      // Convert due date back to family timezone for display
      var dueAtFamilyTz = timeZoneService.ConvertFromUtc(task.DueAt, pet.Family.Timezone);

      // Create TaskReminderDto with family timezone
      var taskReminder = new TaskReminderDto(
        task.Id,
        task.FamilyId,
        task.Title,
        dueAtFamilyTz,
        new List<Guid>() // Will be populated inside SendTaskReminderToFamilyAsync
      );

      // Send reminder to all family members
      await notificationService.SendTaskReminderToFamilyAsync(
        task.FamilyId,
        taskReminder,
        cancellationToken);

      logger.LogInformation(
        "Task reminder notification sent for task {TaskId}",
        task.Id);
    }
    catch (Exception ex)
    {
      logger.LogError(ex,
        "Failed to send task reminder notification for task {TaskId}",
        task.Id);
      // Don't throw - notification failure shouldn't break the flow
    }
  }
}
