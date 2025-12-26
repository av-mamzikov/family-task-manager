using FamilyTaskManager.UseCases.Features.FamilyManagement.Queries;
using FamilyTaskManager.UseCases.Features.TasksManagement.Queries;
using Microsoft.Extensions.Logging;

namespace FamilyTaskManager.UseCases.Features.TasksManagement.Commands;

/// <summary>
///   Command to send reminders for all tasks that are due soon
/// </summary>
public record SendTaskRemindersCommand(DateTime utcFrom, DateTime utcTo) : ICommand<Result>;

public class SendTaskRemindersHandler(
  IMediator mediator,
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

    var tasksResult = await mediator.Send(new GetTasksDueForReminderQuery(command.utcFrom, command.utcTo),
      cancellationToken);

    if (!tasksResult.IsSuccess) return Result.Error("Failed to retrieve tasks");

    var tasksByFamily = tasksResult.Value
      .GroupBy(t => t.FamilyId)
      .ToList();

    foreach (var family in families)
      try
      {
        var tasks = tasksByFamily
          .FirstOrDefault(g => g.Key == family.Id)?.ToList() ?? [];

        foreach (var task in tasks)
        {
          await mediator.Send(new TriggerTaskReminderCommand(task.TaskId), cancellationToken);
          totalRemindersSent++;
        }
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "Error processing reminders for family {FamilyId}", family.Id);
      }

    logger.LogInformation("Sent {Count} task reminders across {FamilyCount} families", totalRemindersSent,
      families.Count);
    return Result.Success();
  }
}
