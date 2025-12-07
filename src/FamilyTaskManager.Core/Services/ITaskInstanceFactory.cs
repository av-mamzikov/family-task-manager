using FamilyTaskManager.Core.SpotAggregate;
using FamilyTaskManager.Core.TaskAggregate;

namespace FamilyTaskManager.Core.Services;

/// <summary>
///   Domain service for creating task instances from templates.
///   Encapsulates business rules for task instance creation and validation.
/// </summary>
public interface ITaskInstanceFactory
{
  /// <summary>
  ///   Creates a task instance from a template if business rules are satisfied.
  /// </summary>
  /// <param name="template">The template to create instance from</param>
  /// <param name="spotBowsing">The Spot entity (with Family loaded) for the task</param>
  /// <param name="dueAt">The due date for the task instance</param>
  /// <param name="existingInstances">Existing task instances for this template</param>
  /// <returns>Result containing the created task instance or error</returns>
  Result<TaskInstance> CreateFromTemplate(TaskTemplate template, SpotBowsing spotBowsing, DateTime dueAt,
    IEnumerable<TaskInstance> existingInstances);
}
