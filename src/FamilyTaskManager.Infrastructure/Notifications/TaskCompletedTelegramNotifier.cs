using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.TaskAggregate.Events;
using FamilyTaskManager.UseCases.Families.Specifications;
using Mediator;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace FamilyTaskManager.Infrastructure.Notifications;

/// <summary>
///   Sends TaskCompletedEvent notifications to Telegram.
///   Used by OutboxDispatcherJob.
/// </summary>
public class TaskCompletedTelegramNotifier(
  ITelegramBotClient telegramClient,
  IRepository<Family> familyRepository,
  ILogger<TaskCompletedTelegramNotifier> logger)
  : INotificationHandler<TaskCompletedEvent>
{
  public async ValueTask Handle(TaskCompletedEvent notification, CancellationToken cancellationToken)
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
        logger.LogWarning("No active members in family {FamilyId}, skipping notification", family.Id);
        return;
      }

      // Format message using data from event
      var message = $"✅ <b>Задача выполнена!</b>\n\n" +
                    $"👤 {notification.CompletedByUserName}\n" +
                    $"📋 {notification.Title}\n" +
                    $"⭐ Баллы: {notification.Points}";

      // Send to all active family members
      var sendTasks = activeMembers.Select(m =>
        telegramClient.SendTextMessageAsync(
          m.User.TelegramId,
          message,
          parseMode: ParseMode.Html,
          cancellationToken: cancellationToken));

      await Task.WhenAll(sendTasks);

      logger.LogInformation(
        "Sent task completed notification for task {TaskId} to family {FamilyId}",
        notification.TaskId, family.Id);
    }
    catch (Exception ex)
    {
      logger.LogError(ex,
        "Failed to send task completed notification for task {TaskId}",
        notification.TaskId);
      throw;
    }
  }
}
