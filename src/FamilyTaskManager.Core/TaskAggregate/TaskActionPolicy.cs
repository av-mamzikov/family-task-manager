using FamilyTaskManager.Core.TaskAggregate.DTOs;

namespace FamilyTaskManager.Core.TaskAggregate;

public static class TaskActionPolicy
{
  public static IReadOnlyCollection<TaskAction> GetAvailableActions(TaskInstance task, Guid? actorUserId) =>
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
    List<TaskAction> actions = [];
    if (CanTake(status, assignedToUserId, actorUserId))
      actions.Add(TaskAction.Take);
    if (CanComplete(status, assignedToUserId, actorUserId))
      actions.Add(TaskAction.Complete);
    if (CanRefuse(status, assignedToUserId, actorUserId))
      actions.Add(TaskAction.Refuse);
    if (CanDelete(status, assignedToUserId, actorUserId))
      actions.Add(TaskAction.Delete);

    return [..actions];
  }

  private static bool CanTake(TaskStatus status, Guid? assignedToUserId, Guid? actorUserId) =>
    status == TaskStatus.Active
    || (status == TaskStatus.InProgress
        && (assignedToUserId == null || !IsAssignedToActor(assignedToUserId, actorUserId)));

  private static bool CanComplete(TaskStatus status, Guid? assignedToUserId, Guid? actorUserId) =>
    IsAssignedToActor(assignedToUserId, actorUserId) && IsInProgress(status);

  private static bool CanRefuse(TaskStatus status, Guid? assignedToUserId, Guid? actorUserId) =>
    IsAssignedToActor(assignedToUserId, actorUserId) && IsInProgress(status);

  private static bool CanDelete(TaskStatus status, Guid? assignedToUserId, Guid? actorUserId) =>
    IsAssignedToActor(assignedToUserId, actorUserId) && IsInProgress(status);

  private static bool IsInProgress(TaskStatus status) => status == TaskStatus.InProgress;

  private static bool IsAssignedToActor(Guid? assignedToUserId, Guid? actorUserId) =>
    assignedToUserId == actorUserId;
}
