using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.UserAggregate;
using FamilyTaskManager.UseCases.Families.Specifications;
using FamilyTaskManager.UseCases.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace FamilyTaskManager.Infrastructure.Notifications;

/// <summary>
///   Service for sending Telegram notifications to family members
/// </summary>
public class TelegramNotificationService(
  ITelegramBotClient botClient,
  IRepository<Family> familyRepository,
  IRepository<User> userRepository,
  ILogger<TelegramNotificationService> logger) : ITelegramNotificationService
{
  public async Task SendTaskReminderAsync(long telegramId, TaskReminderDto task,
    CancellationToken cancellationToken = default)
  {
    try
    {
      var message = $"⏰ <b>Напоминание о задаче</b>\n\n" +
                    $"📝 {EscapeHtml(task.Title)}\n" +
                    $"⏳ Срок: {task.DueAt:dd.MM.yyyy HH:mm}\n\n" +
                    $"Не забудьте выполнить задачу вовремя! 🎯";

      await botClient.SendTextMessageAsync(
        telegramId,
        message,
        parseMode: ParseMode.Html,
        cancellationToken: cancellationToken);

      logger.LogInformation(
        "Task reminder sent to TelegramId {TelegramId} for task '{TaskTitle}'",
        telegramId, task.Title);
    }
    catch (Exception ex)
    {
      logger.LogError(ex,
        "Failed to send task reminder to TelegramId {TelegramId} for task '{TaskTitle}'",
        telegramId, task.Title);
      throw;
    }
  }

  public async Task SendTaskReminderToFamilyAsync(Guid familyId, TaskReminderDto task,
    CancellationToken cancellationToken = default)
  {
    try
    {
      // Get family with members
      var spec = new GetFamilyWithMembersSpec(familyId);
      var family = await familyRepository.FirstOrDefaultAsync(spec, cancellationToken);

      if (family == null)
      {
        logger.LogWarning("Family {FamilyId} not found for task reminder", familyId);
        return;
      }

      var activeMembers = family.Members.Where(m => m.IsActive).ToList();

      if (activeMembers.Count == 0)
      {
        logger.LogWarning("No active members found in family {FamilyId} for task reminder", familyId);
        return;
      }

      // Send reminder to each member
      var message = $"⏰ <b>Напоминание о задаче</b>\n\n" +
                    $"📝 {EscapeHtml(task.Title)}\n" +
                    $"⏳ Срок: {task.DueAt:dd.MM.yyyy HH:mm}\n\n" +
                    $"Не забудьте выполнить задачу вовремя! 🎯";

      var tasks = new List<Task>();
      foreach (var member in activeMembers) tasks.Add(SendToUserAsync(member.UserId, message, cancellationToken));

      await Task.WhenAll(tasks);

      logger.LogInformation(
        "Task reminder sent to {MemberCount} members in family {FamilyId} for task '{TaskTitle}'",
        activeMembers.Count, familyId, task.Title);
    }
    catch (Exception ex)
    {
      logger.LogError(ex,
        "Failed to send task reminder to family {FamilyId} for task '{TaskTitle}'",
        familyId, task.Title);
      throw;
    }
  }

  public async Task SendTaskCreatedAsync(Guid familyId, string taskTitle, int points, string petName, DateTime dueAt,
    CancellationToken cancellationToken = default)
  {
    try
    {
      var message = $"📝 <b>Новая задача создана!</b>\n\n" +
                    $"🐾 {EscapeHtml(petName)}\n" +
                    $"📋 {EscapeHtml(taskTitle)}\n" +
                    $"⭐ {points} очков\n" +
                    $"⏳ Срок: {dueAt:dd.MM.yyyy HH:mm}\n\n" +
                    $"Время приступать к работе! 🎯";

      await SendToFamilyMembersAsync(familyId, message, cancellationToken);

      logger.LogInformation(
        "Task created notification sent to family {FamilyId}: '{TaskTitle}' for pet '{PetName}'",
        familyId, taskTitle, petName);
    }
    catch (Exception ex)
    {
      logger.LogError(ex,
        "Failed to send task created notification to family {FamilyId}",
        familyId);
      throw;
    }
  }

  public async Task SendTaskCompletedAsync(Guid familyId, string userName, string taskTitle, int points,
    CancellationToken cancellationToken = default)
  {
    try
    {
      var message = $"✅ <b>Задача выполнена!</b>\n\n" +
                    $"👤 {EscapeHtml(userName)}\n" +
                    $"📝 {EscapeHtml(taskTitle)}\n" +
                    $"⭐ +{points} очков\n\n" +
                    $"Отличная работа! 🎉";

      await SendToFamilyMembersAsync(familyId, message, cancellationToken);

      logger.LogInformation(
        "Task completed notification sent to family {FamilyId}: user '{UserName}' completed '{TaskTitle}'",
        familyId, userName, taskTitle);
    }
    catch (Exception ex)
    {
      logger.LogError(ex,
        "Failed to send task completed notification to family {FamilyId}",
        familyId);
      throw;
    }
  }

  public async Task SendPetMoodChangedAsync(Guid familyId, string petName, int moodScore,
    CancellationToken cancellationToken = default)
  {
    try
    {
      string emoji;
      string status;

      if (moodScore >= 80)
      {
        emoji = "😊";
        status = "отличное";
      }
      else if (moodScore >= 50)
      {
        emoji = "😐";
        status = "нормальное";
      }
      else if (moodScore >= 20)
      {
        emoji = "😟";
        status = "плохое";
      }
      else
      {
        emoji = "😢";
        status = "критическое";
      }

      var message = $"{emoji} <b>Настроение питомца изменилось</b>\n\n" +
                    $"🐾 {EscapeHtml(petName)}\n" +
                    $"💭 Настроение: {status} ({moodScore}/100)\n\n";

      if (moodScore < 20)
      {
        message += "⚠️ Срочно нужно выполнить задачи по уходу за питомцем!";
      }
      else if (moodScore < 50)
      {
        message += "⚡ Не забывайте о задачах по уходу за питомцем!";
      }
      else if (moodScore >= 80)
      {
        message += "🎉 Питомец очень доволен! Продолжайте в том же духе!";
      }

      await SendToFamilyMembersAsync(familyId, message, cancellationToken);

      logger.LogInformation(
        "Pet mood changed notification sent to family {FamilyId}: pet '{PetName}' mood is {MoodScore}",
        familyId, petName, moodScore);
    }
    catch (Exception ex)
    {
      logger.LogError(ex,
        "Failed to send pet mood notification to family {FamilyId}",
        familyId);
      throw;
    }
  }

  public async Task SendPetCreatedAsync(Guid familyId, string petName, string petType,
    CancellationToken cancellationToken = default)
  {
    try
    {
      var emoji = petType.ToLowerInvariant() switch
      {
        "cat" => "🐱",
        "dog" => "🐶",
        "hamster" => "🐹",
        _ => "🐾"
      };

      var petTypeRu = petType.ToLowerInvariant() switch
      {
        "cat" => "кошка",
        "dog" => "собака",
        "hamster" => "хомяк",
        _ => "питомец"
      };

      var message = $"{emoji} <b>Новый питомец в семье!</b>\n\n" +
                    $"🐾 Имя: {EscapeHtml(petName)}\n" +
                    $"📋 Тип: {petTypeRu}\n\n" +
                    $"Добро пожаловать в семью! 🎉";

      await SendToFamilyMembersAsync(familyId, message, cancellationToken);

      logger.LogInformation(
        "Pet created notification sent to family {FamilyId}: pet '{PetName}' ({PetType})",
        familyId, petName, petType);
    }
    catch (Exception ex)
    {
      logger.LogError(ex,
        "Failed to send pet created notification to family {FamilyId}",
        familyId);
      throw;
    }
  }

  public async Task SendPetDeletedAsync(Guid familyId, string petName, CancellationToken cancellationToken = default)
  {
    try
    {
      var message = $"😢 <b>Питомец покинул семью</b>\n\n" +
                    $"🐾 {EscapeHtml(petName)}\n\n" +
                    $"Мы будем скучать! 💔";

      await SendToFamilyMembersAsync(familyId, message, cancellationToken);

      logger.LogInformation(
        "Pet deleted notification sent to family {FamilyId}: pet '{PetName}'",
        familyId, petName);
    }
    catch (Exception ex)
    {
      logger.LogError(ex,
        "Failed to send pet deleted notification to family {FamilyId}",
        familyId);
      throw;
    }
  }

  public async Task SendMemberJoinedAsync(Guid familyId, string userName, CancellationToken cancellationToken = default)
  {
    try
    {
      var message = $"👋 <b>Новый участник присоединился к семье!</b>\n\n" +
                    $"👤 {EscapeHtml(userName)}\n\n" +
                    $"Добро пожаловать в семью! 🎉";

      await SendToFamilyMembersAsync(familyId, message, cancellationToken);

      logger.LogInformation(
        "Member joined notification sent to family {FamilyId}: user '{UserName}' joined",
        familyId, userName);
    }
    catch (Exception ex)
    {
      logger.LogError(ex,
        "Failed to send member joined notification to family {FamilyId}",
        familyId);
      throw;
    }
  }

  /// <summary>
  ///   Send message to all active members of a family
  /// </summary>
  private async Task SendToFamilyMembersAsync(Guid familyId, string message, CancellationToken cancellationToken)
  {
    // Get family with members
    var spec = new GetFamilyWithMembersSpec(familyId);
    var family = await familyRepository.FirstOrDefaultAsync(spec, cancellationToken);

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
    foreach (var member in activeMembers)
    {
      tasks.Add(SendToUserAsync(member.UserId, message, cancellationToken));
    }

    await Task.WhenAll(tasks);
  }

  /// <summary>
  ///   Send message to a specific user by userId
  /// </summary>
  private async Task SendToUserAsync(Guid userId, string message, CancellationToken cancellationToken)
  {
    try
    {
      var user = await userRepository.GetByIdAsync(userId, cancellationToken);
      if (user == null)
      {
        logger.LogWarning("User {UserId} not found for notification", userId);
        return;
      }

      await botClient.SendTextMessageAsync(
        user.TelegramId,
        message,
        parseMode: ParseMode.Html,
        cancellationToken: cancellationToken);

      logger.LogDebug("Notification sent to user {UserId} (TelegramId: {TelegramId})", userId, user.TelegramId);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Failed to send notification to user {UserId}", userId);
      // Don't throw - we want to continue sending to other users
    }
  }

  /// <summary>
  ///   Escape HTML special characters for Telegram HTML parse mode
  /// </summary>
  private static string EscapeHtml(string text) =>
    text
      .Replace("&", "&amp;")
      .Replace("<", "&lt;")
      .Replace(">", "&gt;");
}
