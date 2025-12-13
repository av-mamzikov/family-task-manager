using FamilyTaskManager.UseCases.Features.FamilyManagement.Queries;
using FamilyTaskManager.UseCases.Features.TasksManagement.Queries;
using Microsoft.Extensions.Logging;

namespace FamilyTaskManager.UseCases.Features.TasksManagement.Commands;

/// <summary>
///   Command to send reminders for all tasks that are due soon
/// </summary>
public record SendTaskRemindersCommand : ICommand<Result>;

public class SendTaskRemindersHandler(
  IMediator mediator,
  ITimeZoneService timeZoneService,
  ILogger<SendTaskRemindersHandler> logger)
  : ICommandHandler<SendTaskRemindersCommand, Result>
{
  public async ValueTask<Result> Handle(SendTaskRemindersCommand command, CancellationToken cancellationToken)
  {
    // Get all active families to process reminders in their respective timezones
    var familiesResult = await mediator.Send(new GetActiveFamiliesQuery(), cancellationToken);

    if (!familiesResult.IsSuccess) return Result.Error("Failed to retrieve active families");

    var families = familiesResult.Value;
    var totalRemindersSent = 0;

    foreach (var family in families)
      try
      {
        // Get current time in family timezone
        var familyNow = timeZoneService.GetCurrentTimeInTimeZone(family.Timezone);
        var oneHourFromNow = familyNow.AddHours(1);

        // Convert back to UTC for database query
        var utcFrom = timeZoneService.ConvertToUtc(familyNow, family.Timezone);
        var utcTo = timeZoneService.ConvertToUtc(oneHourFromNow, family.Timezone);

        var tasksResult = await mediator.Send(
          new GetTasksDueForReminderQuery(utcFrom, utcTo),
          cancellationToken);

        if (!tasksResult.IsSuccess) continue; // Skip this family but continue with others

        var tasks = tasksResult.Value.Where(t => t.FamilyId == family.Id).ToList();

        // Trigger reminder for each task (will register domain events)
        foreach (var task in tasks)
        {
          await mediator.Send(new TriggerTaskReminderCommand(task.TaskId), cancellationToken);
          totalRemindersSent++;
        }
      }
      catch (Exception ex)
      {
        // Log error but continue processing other families
        logger.LogError(ex, "Error processing reminders for family {FamilyId}", family.Id);
      }

    logger.LogInformation("Sent {Count} task reminders across {FamilyCount} families", totalRemindersSent,
      families.Count);
    return Result.Success();
  }
}
