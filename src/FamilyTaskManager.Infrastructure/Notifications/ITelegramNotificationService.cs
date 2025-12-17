namespace FamilyTaskManager.Infrastructure.Notifications;

/// <summary>
///   Service for sending Telegram notifications to family members.
///   Used by domain event handlers that send notifications immediately (not via Outbox).
/// </summary>
public interface ITelegramNotificationService
{
  Task SendToFamilyMembersAsync(Guid familyId, string message, Guid[] excludeUserId,
    CancellationToken cancellationToken);

  Task SendToUserAsync(long telegramId, string message, CancellationToken cancellationToken);
}
