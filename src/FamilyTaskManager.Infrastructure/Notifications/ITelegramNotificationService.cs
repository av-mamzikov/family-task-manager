using FamilyTaskManager.Core.TaskAggregate;
using FamilyTaskManager.Infrastructure.Notifications.Handlers;

namespace FamilyTaskManager.Infrastructure.Notifications;

/// <summary>
///   Service for sending Telegram notifications to family members
/// </summary>
public interface ITelegramNotificationService
{
  Task SendTaskReminderAsync(long telegramId, TaskReminderDto task, CancellationToken cancellationToken = default);

  Task SendTaskReminderToFamilyAsync(Guid familyId, TaskReminderDto task,
    CancellationToken cancellationToken = default);

  Task SendTaskCreatedAsync(Guid familyId, string taskTitle, TaskPoints points, string petName, DateTime dueAt,
    CancellationToken cancellationToken = default);

  Task SendTaskStartedAsync(Guid familyId, Guid userId, string userName, string taskTitle, TaskPoints points,
    CancellationToken cancellationToken = default);

  Task SendTaskCompletedAsync(Guid familyId, Guid userId, string userName, string taskTitle, TaskPoints points,
    CancellationToken cancellationToken = default);

  Task SendPetMoodChangedAsync(Guid familyId, string petName, int moodScore,
    CancellationToken cancellationToken = default);

  Task SendPetCreatedAsync(Guid familyId, string petName, string petType,
    CancellationToken cancellationToken = default);

  Task SendPetDeletedAsync(Guid familyId, string petName, CancellationToken cancellationToken = default);

  Task SendMemberJoinedAsync(Guid familyId, string userName, CancellationToken cancellationToken = default);

  Task SendTaskCreatedBatchAsync(Guid familyId, List<TaskCreatedNotificationDto> tasks,
    CancellationToken cancellationToken = default);

  Task SendTaskReminderBatchAsync(Guid familyId, List<TaskReminderBatchDto> reminders,
    CancellationToken cancellationToken = default);
}
