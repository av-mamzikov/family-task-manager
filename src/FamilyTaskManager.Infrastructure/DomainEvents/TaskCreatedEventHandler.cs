using FamilyTaskManager.Core.Interfaces;
using FamilyTaskManager.Core.PetAggregate;
using FamilyTaskManager.Core.TaskAggregate.Events;
using FamilyTaskManager.Infrastructure.Notifications;
using FamilyTaskManager.UseCases.Pets.Specifications;
using Mediator;

namespace FamilyTaskManager.Infrastructure.DomainEvents;

public class TaskCreatedEventHandler(
  ILogger<TaskCreatedEventHandler> logger,
  ITelegramNotificationService notificationService,
  IRepository<Pet> petRepository,
  ITimeZoneService timeZoneService)
  : INotificationHandler<TaskCreatedEvent>
{
  public async ValueTask Handle(TaskCreatedEvent notification, CancellationToken cancellationToken)
  {
    var task = notification.Task;

    logger.LogInformation("Task created: {TaskId} - {TaskTitle}",
      task.Id, task.Title);

    try
    {
      // Get pet information with family data
      var spec = new GetPetWithFamilySpec(task.PetId);
      var pet = await petRepository.FirstOrDefaultAsync(spec, cancellationToken);
      if (pet == null)
      {
        logger.LogWarning("Pet {PetId} not found for task {TaskId} notification",
          task.PetId, task.Id);
        return;
      }

      // Convert due date back to family timezone for display
      var dueAtFamilyTz = timeZoneService.ConvertFromUtc(task.DueAt, pet.Family.Timezone);

      // Send notification to all family members
      await notificationService.SendTaskCreatedAsync(
        task.FamilyId,
        task.Title,
        task.Points,
        pet.Name,
        dueAtFamilyTz,
        cancellationToken);

      logger.LogInformation(
        "Task creation notification sent for task {TaskId}",
        task.Id);
    }
    catch (Exception ex)
    {
      logger.LogError(ex,
        "Failed to send task creation notification for task {TaskId}",
        task.Id);
      // Don't throw - notification failure shouldn't break the flow
    }
  }
}
