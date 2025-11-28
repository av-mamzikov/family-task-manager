using FamilyTaskManager.UseCases.TaskTemplates;

namespace FamilyTaskManager.UseCases.Tasks.Specifications;

/// <summary>
///   Specification to get active task templates with projection to DTO for SQL-level SELECT.
/// </summary>
public class ActiveTaskTemplatesWithTimeZoneSpec : Specification<TaskTemplate, TaskTemplateDto>
{
  public ActiveTaskTemplatesWithTimeZoneSpec()
  {
    Query
      .Where(t => t.IsActive)
      .Select(TaskTemplateDto.Projections.FromTaskTemplate);
  }
}
