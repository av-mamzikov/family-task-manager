namespace FamilyTaskManager.Core.FamilyAggregate.Events;

public sealed class DailyOverdueTasksSummaryDueEvent : DomainEventBase
{
  public required Guid FamilyId { get; init; }
  public required string Timezone { get; init; } = string.Empty;

  public required Guid UserId { get; init; }
  public required string UserName { get; init; } = string.Empty;
  public required long UserTelegramId { get; init; }

  public required int OverdueTasksCount { get; init; }
}
