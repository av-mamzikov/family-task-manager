using FamilyTaskManager.Core.FamilyAggregate.Specifications;
using FamilyTaskManager.Core.TaskAggregate.Specifications;
using FamilyTaskManager.UseCases.Features.TasksManagement.Services;
using Microsoft.Extensions.Logging;

namespace FamilyTaskManager.UseCases.Features.TasksManagement.Commands;

public record SendDailyOverdueTasksRemindersCommand(DateTime? PreviousFireTimeUtc, DateTime CurrentFireTimeUtc)
  : ICommand<Result>;

public class SendDailyOverdueTasksRemindersHandler(
  IAppReadRepository<Family> familyRepository,
  IAppRepository<TaskInstance> taskAppRepository,
  IAppRepository<Family> familyAppRepository,
  ITimeZoneService timeZoneService,
  ILogger<SendDailyOverdueTasksRemindersHandler> logger)
  : ICommandHandler<SendDailyOverdueTasksRemindersCommand, Result>
{
  public async ValueTask<Result> Handle(SendDailyOverdueTasksRemindersCommand command,
    CancellationToken cancellationToken)
  {
    var families = await familyRepository.ListAsync(new ActiveFamiliesSpec(), cancellationToken);
    var totalEvents = 0;
    foreach (var family in families)
      try
      {
        var familyEvents = 0;
        var shouldNotify = DailyReminderWindowCalculator.CrossedLocalTimeBetween(
          command.PreviousFireTimeUtc,
          command.CurrentFireTimeUtc,
          family.Timezone,
          timeZoneService,
          19);

        if (!shouldNotify)
          continue;

        var utcNow = command.CurrentFireTimeUtc;

        var overdueTasks = await taskAppRepository.ListAsync(
          new OverdueAssignedTasksSpec(family.Id, utcNow),
          cancellationToken);

        if (overdueTasks.Count == 0)
          continue;

        var overdueCountsByUserId = overdueTasks
          .Where(t => t.AssignedToMember?.IsActive == true)
          .GroupBy(t => t.AssignedToMember!.UserId)
          .ToDictionary(g => g.Key, g => g.Count());

        if (overdueCountsByUserId.Count == 0)
          continue;

        foreach (var member in family.Members.Where(m => m.IsActive))
        {
          if (!overdueCountsByUserId.TryGetValue(member.UserId, out var count) || count <= 0)
            continue;

          family.RegisterDailyOverdueTasksSummary(member, count);
          familyEvents++;
        }

        if (familyEvents > 0)
        {
          await familyAppRepository.UpdateAsync(family, cancellationToken);
          totalEvents += familyEvents;
        }
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "Error processing daily overdue reminders for family {FamilyId}", family.Id);
      }

    logger.LogInformation("Daily overdue reminders: published {Count} events", totalEvents);
    return Result.Success();
  }
}
