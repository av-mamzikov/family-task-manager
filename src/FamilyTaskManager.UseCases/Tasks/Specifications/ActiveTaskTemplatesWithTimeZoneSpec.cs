using System.Linq.Expressions;

namespace FamilyTaskManager.UseCases.Tasks.Specifications;

public record TaskTemplateDto(Guid Id, Guid FamilyId, string Title, string Schedule, string timeZone)
{
  public static class Projections
  {
    public static readonly Expression<Func<TaskTemplate, TaskTemplateDto>> FromTaskTemplate =
      t => new TaskTemplateDto(t.Id, t.FamilyId, t.Title, t.Schedule, t.Family.Timezone);
  }
}

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
