using FamilyTaskManager.Core.TaskAggregate.DTOs;

namespace FamilyTaskManager.Core.TaskAggregate;

public static class TaskActionPolicy
{
  public static IReadOnlyCollection<TaskAction> GetAvailableActions(TaskInstance task, Guid actorUserId) =>
    GetAvailableActions(task.Status, task.AssignedToMember?.UserId, actorUserId);

  public static IReadOnlyCollection<TaskAction> GetAvailableActions(TaskDto task, Guid actorUserId) =>
    GetAvailableActions(task.Status, task.AssignedToUserId, actorUserId);

  public static IReadOnlyCollection<TaskAction> GetAvailableActions(
    TaskStatus status,
    Guid? assignedToUserId,
    Guid? actorUserId)
  {
    if (actorUserId == Guid.Empty)
      return [];

    return status switch
    {
      TaskStatus.Active => CanTake(assignedToUserId, actorUserId)
        ? new[] { TaskAction.Take }
        : [],

      TaskStatus.InProgress => IsAssignedToActor(assignedToUserId, actorUserId)
        ? new[] { TaskAction.Complete, TaskAction.Refuse, TaskAction.Delete }
        : [],

      _ => []
    };
  }

  private static bool CanTake(Guid? assignedToUserId, Guid? actorUserId) =>
    assignedToUserId is null || assignedToUserId == actorUserId;

  private static bool IsAssignedToActor(Guid? assignedToUserId, Guid? actorUserId) =>
    assignedToUserId == actorUserId;
}
