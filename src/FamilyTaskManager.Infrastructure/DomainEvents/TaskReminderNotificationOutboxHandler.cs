using System.Text.Json;
using FamilyTaskManager.Core.Interfaces;
using FamilyTaskManager.Core.PetAggregate;
using FamilyTaskManager.Core.TaskAggregate.Events;
using FamilyTaskManager.Infrastructure.Notifications;
using FamilyTaskManager.Infrastructure.Notifications.Payloads;
using FamilyTaskManager.UseCases.Pets.Specifications;
using Mediator;

namespace FamilyTaskManager.Infrastructure.DomainEvents;

/// <summary>
///   Handler for TaskReminderDueEvent that queues notifications in outbox for batched delivery.
/// </summary>
public class TaskReminderNotificationOutboxHandler(
  IInfrastructureRepository<DomainEventOutbox> outboxRepository,
  IRepository<Pet> petRepository,
  ITimeZoneService timeZoneService,
  ILogger<TaskReminderNotificationOutboxHandler> logger)
  : INotificationHandler<TaskReminderDueEvent>
{
  public async ValueTask Handle(TaskReminderDueEvent notification, CancellationToken cancellationToken)
  {
    var task = notification.Task;

    logger.LogInformation("Processing TaskReminderDueEvent for task {TaskId}", task.Id);

    try
    {
      // Get pet information with family data for timezone conversion
      var spec = new GetPetWithFamilySpec(task.PetId);
      var pet = await petRepository.FirstOrDefaultAsync(spec, cancellationToken);
      if (pet == null)
      {
        logger.LogWarning("Pet {PetId} not found for task {TaskId}", task.PetId, task.Id);
        return;
      }

      // Convert due date to family timezone for display
      var dueAtFamilyTz = timeZoneService.ConvertFromUtc(task.DueAt, pet.Family.Timezone);

      // Create payload with all data needed for notification
      var payload = new TaskReminderNotificationPayload
      {
        FamilyId = task.FamilyId,
        TaskId = task.Id,
        TaskTitle = task.Title,
        DueAtFamilyTz = dueAtFamilyTz
      };

      // Write to outbox for batched delivery
      var outboxEntry = new DomainEventOutbox
      {
        Id = Guid.NewGuid(),
        EventType = nameof(TaskReminderDueEvent),
        Payload = JsonSerializer.Serialize(payload),
        DeliveryMode = DeliveryMode.Batched,
        OccurredAtUtc = DateTime.UtcNow,
        Status = NotificationStatus.Pending,
        Attempts = 0
      };

      await outboxRepository.AddAsync(outboxEntry, cancellationToken);
      // Note: SaveChangesAsync will be called by the main UseCase/Command handler
      // to ensure all changes (including outbox entries) are saved in a single transaction

      logger.LogInformation(
        "Task reminder notification queued in outbox for task {TaskId}",
        task.Id);
    }
    catch (Exception ex)
    {
      logger.LogError(ex,
        "Failed to queue task reminder notification in outbox for task {TaskId}",
        task.Id);
      // Don't throw - notification failure shouldn't break the flow
    }
  }
}
