using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.UserAggregate;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace FamilyTaskManager.Infrastructure.Notifications;

/// <summary>
///   Service for sending Telegram notifications to family members
/// </summary>
public class TelegramNotificationService(
  ITelegramBotClient botClient,
  IAppRepository<Family> familyAppRepository,
  IAppRepository<User> userAppRepository,
  ILogger<TelegramNotificationService> logger) : ITelegramNotificationService
{
  /// <summary>
  ///   Send message to all active members of a family
  /// </summary>
  public async Task SendToFamilyMembersAsync(Guid familyId, string message, Guid[] excludeUserId,
    CancellationToken cancellationToken)
  {
    // Get family with members
    var family = await familyAppRepository.GetByIdAsync(familyId, cancellationToken);

    if (family == null)
    {
      logger.LogWarning("Family {FamilyId} not found for notification", familyId);
      return;
    }

    var activeMembers = family.Members.Where(m => m.IsActive).ToList();

    if (activeMembers.Count == 0)
    {
      logger.LogWarning("No active members found in family {FamilyId}", familyId);
      return;
    }

    // Send to each member
    var tasks = new List<Task>();
    foreach (var member in activeMembers.Where(m => !excludeUserId.Contains(m.UserId)))
      tasks.Add(SendToUserAsync(member.UserId, message, cancellationToken));

    await Task.WhenAll(tasks);
  }

  /// <summary>
  ///   Send message to a specific user by userId
  /// </summary>
  public async Task SendToUserAsync(Guid userId, string message, CancellationToken cancellationToken)
  {
    try
    {
      var user = await userAppRepository.GetByIdAsync(userId, cancellationToken);
      if (user == null)
      {
        logger.LogWarning("User {UserId} not found for notification", userId);
        return;
      }

      await botClient.SendTextMessageAsync(
        user.TelegramId,
        message,
        parseMode: ParseMode.Markdown,
        cancellationToken: cancellationToken);

      logger.LogDebug("Notification sent to user {UserId} (TelegramId: {TelegramId})", userId, user.TelegramId);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Failed to send notification to user {UserId}", userId);
      // Don't throw - we want to continue sending to other users
    }
  }
}
