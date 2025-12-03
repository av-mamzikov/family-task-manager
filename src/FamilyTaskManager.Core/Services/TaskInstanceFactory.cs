using FamilyTaskManager.Core.PetAggregate;
using FamilyTaskManager.Core.TaskAggregate;
using TaskStatus = FamilyTaskManager.Core.TaskAggregate.TaskStatus;

namespace FamilyTaskManager.Core.Services;

/// <summary>
///   Domain service for creating task instances from templates.
///   Encapsulates business rules for task instance creation and validation.
/// </summary>
public class TaskInstanceFactory : ITaskInstanceFactory
{
  public Result<TaskInstance> CreateFromTemplate(TaskTemplate template, Pet pet, DateTime dueAt,
    IEnumerable<TaskInstance> existingInstances)
  {
    Guard.Against.Null(pet);

    // Check if there's already an active TaskInstance for this template
    var existingInstancesList = existingInstances?.ToList() ?? new List<TaskInstance>();
    var activeInstance = existingInstancesList.FirstOrDefault(t => t.Status != TaskStatus.Completed);

    if (activeInstance != null) return Result.Error($"Active TaskInstance already exists for template {template.Id}");

    var taskInstance = new TaskInstance(
      pet,
      template.Title,
      template.Points,
      TaskType.Recurring,
      dueAt,
      template.Id
    );

    return Result.Success(taskInstance);
  }
}
