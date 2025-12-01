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
///   Handler for TaskCreatedEvent that queues notifications in outbox for batched delivery.
/// </summary>
public class TaskCreatedNotificationOutboxHandler(
  IInfrastructureRepository<DomainEventOutbox> outboxRepository,
  IRepository<Pet> petRepository,
  ITimeZoneService timeZoneService,
  ILogger<TaskCreatedNotificationOutboxHandler> logger)
  : INotificationHandler<TaskCreatedEvent>
{
  public async ValueTask Handle(TaskCreatedEvent notification, CancellationToken cancellationToken)
  {
    var task = notification.Task;

    logger.LogInformation("Processing TaskCreatedEvent for task {TaskId}", task.Id);

    try
    {
      // Get pet information with family data
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
      var payload = new TaskCreatedNotificationPayload
      {
        FamilyId = task.FamilyId,
        TaskId = task.Id,
        TaskTitle = task.Title,
        Points = task.Points.ToString(),
        PetName = pet.Name,
        DueAtFamilyTz = dueAtFamilyTz
      };

      // Write to outbox for batched delivery
      var outboxEntry = new DomainEventOutbox
      {
        Id = Guid.NewGuid(),
        EventType = nameof(TaskCreatedEvent),
        Payload = JsonSerializer.Serialize(payload),
        DeliveryMode = DeliveryMode.Batched,
        OccurredAtUtc = DateTime.UtcNow,
        Status = NotificationStatus.Pending,
        Attempts = 0
      };

      await outboxRepository.AddAsync(outboxEntry, cancellationToken);
      await outboxRepository.SaveChangesAsync(cancellationToken);

      logger.LogInformation(
        "Task creation notification queued in outbox for task {TaskId}",
        task.Id);
    }
    catch (Exception ex)
    {
      logger.LogError(ex,
        "Failed to queue task creation notification in outbox for task {TaskId}",
        task.Id);
      // Don't throw - notification failure shouldn't break the flow
    }
  }
}
