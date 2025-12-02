using System.Text.Json;
using FamilyTaskManager.Infrastructure.Notifications;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FamilyTaskManager.Infrastructure.Data;

/// <summary>
///   Intercepts SaveChanges to automatically save domain events to Outbox table.
///   This ensures reliable delivery of notifications even after service restarts.
/// </summary>
public class OutboxInterceptor(ILogger<OutboxInterceptor> logger) : SaveChangesInterceptor
{
  public override InterceptionResult<int> SavingChanges(
    DbContextEventData eventData,
    InterceptionResult<int> result)
  {
    AddDomainEventsToOutbox(eventData);
    return base.SavingChanges(eventData, result);
  }

  public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
    DbContextEventData eventData,
    InterceptionResult<int> result,
    CancellationToken cancellationToken = default)
  {
    AddDomainEventsToOutbox(eventData);
    return await base.SavingChangesAsync(eventData, result, cancellationToken);
  }

  private void AddDomainEventsToOutbox(DbContextEventData eventData)
  {
    var context = eventData.Context;
    if (context is not AppDbContext appDbContext) return;

    var entitiesWithEvents = appDbContext.ChangeTracker.Entries<HasDomainEventsBase>()
      .Select(e => e.Entity)
      .SelectMany(e => e.DomainEvents);

    foreach (var domainEvent in entitiesWithEvents)
    {
      var payload = CreateOutboxPayload(domainEvent);

      var outboxEntry = new DomainEventOutbox
      {
        Id = Guid.NewGuid(),
        EventType = domainEvent.GetType().Name,
        Payload = payload,
        OccurredAtUtc = DateTime.UtcNow,
        Status = NotificationStatus.Pending,
        Attempts = 0
      };

      appDbContext.DomainEventOutbox.Add(outboxEntry);

      logger.LogDebug(
        "Domain event {EventType} saved to outbox", domainEvent.GetType().Name);
    }
  }

  /// <summary>
  ///   Serializes domain event directly to JSON.
  ///   Events now contain all necessary data (no entity references).
  /// </summary>
  private static string CreateOutboxPayload(IDomainEvent domainEvent) =>
    JsonSerializer.Serialize(domainEvent, domainEvent.GetType());
}
