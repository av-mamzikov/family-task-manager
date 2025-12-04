using FamilyTaskManager.UseCases.Contracts.TaskTemplates;

namespace FamilyTaskManager.UseCases.Tasks.Specifications;

/// <summary>
///   Specification to get active task templates with projection to DTO for SQL-level SELECT.
/// </summary>
public class ActiveTaskTemplatesDtoSpec : Specification<TaskTemplate, TaskTemplateDto>
{
  public ActiveTaskTemplatesDtoSpec()
  {
    Query.Select(TaskTemplateDto.Projections.FromTaskTemplate);
  }
}
