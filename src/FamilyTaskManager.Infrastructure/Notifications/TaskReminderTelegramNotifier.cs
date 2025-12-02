using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.Interfaces;
using FamilyTaskManager.Core.TaskAggregate.Events;
using FamilyTaskManager.UseCases.Families.Specifications;
using Mediator;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace FamilyTaskManager.Infrastructure.Notifications;

/// <summary>
///   Sends TaskReminderDueEvent notifications to Telegram.
///   Used by OutboxDispatcherJob - enriches data and sends immediately.
/// </summary>
public class TaskReminderTelegramNotifier(
  ITelegramBotClient telegramClient,
  IRepository<Family> familyRepository,
  ITimeZoneService timeZoneService,
  ILogger<TaskReminderTelegramNotifier> logger)
  : INotificationHandler<TaskReminderDueEvent>
{
  public async ValueTask Handle(TaskReminderDueEvent notification, CancellationToken cancellationToken)
  {
    try
    {
      // Get family with members
      var familySpec = new GetFamilyWithMembersSpec(notification.FamilyId);
      var family = await familyRepository.FirstOrDefaultAsync(familySpec, cancellationToken);
      if (family == null)
      {
        logger.LogWarning("Family {FamilyId} not found, skipping notification", notification.FamilyId);
        return;
      }

      var activeMembers = family.Members.Where(m => m.IsActive).ToList();
      if (activeMembers.Count == 0)
      {
        logger.LogWarning("No active members in family {FamilyId}, skipping reminder",
          family.Id);
        return;
      }

      // Convert DueAt from UTC to family timezone for display
      var dueAtLocal = timeZoneService.ConvertFromUtc(notification.DueAt, notification.Timezone);

      // Format message using data from event
      var message = $"⏰ <b>Напоминание о задаче!</b>\n\n" +
                    $"Задача: {notification.Title}\n" +
                    $"Срок: {dueAtLocal:dd.MM.yyyy HH:mm}";

      // Send to all active family members
      var sendTasks = activeMembers.Select(member =>
        telegramClient.SendTextMessageAsync(
          member.User.TelegramId,
          message,
          parseMode: ParseMode.Html,
          cancellationToken: cancellationToken));

      await Task.WhenAll(sendTasks);

      logger.LogInformation(
        "Sent task reminder notification for task {TaskId} to family {FamilyId}",
        notification.TaskId, family.Id);
    }
    catch (Exception ex)
    {
      logger.LogError(ex,
        "Failed to send task reminder notification for task {TaskId}",
        notification.TaskId);
      throw;
    }
  }
}
