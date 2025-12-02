using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.Interfaces;
using FamilyTaskManager.Core.TaskAggregate.Events;
using FamilyTaskManager.UseCases.Families.Specifications;
using Mediator;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace FamilyTaskManager.Infrastructure.Notifications;

/// <summary>
///   Sends TaskCreatedEvent notifications to Telegram.
///   Used by OutboxDispatcherJob - enriches data and sends immediately.
/// </summary>
public class TaskCreatedTelegramNotifier(
  ITelegramBotClient telegramClient,
  IRepository<Family> familyRepository,
  ITimeZoneService timeZoneService,
  ILogger<TaskCreatedTelegramNotifier> logger)
  : INotificationHandler<TaskCreatedEvent>
{
  public async ValueTask Handle(TaskCreatedEvent notification, CancellationToken cancellationToken)
  {
    try
    {
      // Get family with members (only need this for sending)
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
        logger.LogWarning("No active members in family {FamilyId}, skipping notification", family.Id);
        return;
      }

      // Convert DueAt from UTC to family timezone for display
      var dueAtLocal = timeZoneService.ConvertFromUtc(notification.DueAt, notification.Timezone);

      // Format message using data from event
      var message = $"📋 <b>Новая задача для {notification.PetName}!</b>\n\n" +
                    $"Задача: {notification.Title}\n" +
                    $"Баллы: {notification.Points}\n" +
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
        "Sent task created notification for task {TaskId} to family {FamilyId}",
        notification.TaskId, family.Id);
    }
    catch (Exception ex)
    {
      logger.LogError(ex,
        "Failed to send task created notification for task {TaskId}",
        notification.TaskId);
      throw;
    }
  }
}
