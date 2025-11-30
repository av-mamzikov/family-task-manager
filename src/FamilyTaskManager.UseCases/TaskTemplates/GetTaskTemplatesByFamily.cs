using System.Linq.Expressions;
using FamilyTaskManager.UseCases.TaskTemplates.Specifications;

namespace FamilyTaskManager.UseCases.TaskTemplates;

public record TaskTemplateDto(
  Guid Id,
  Guid FamilyId,
  string Title,
  TaskPoints Points,
  ScheduleType ScheduleType,
  TimeOnly ScheduleTime,
  DayOfWeek? ScheduleDayOfWeek,
  int? ScheduleDayOfMonth,
  Guid PetId,
  string PetName,
  DateTime CreatedAt,
  TimeSpan DueDuration)
{
  public static class Projections
  {
    public static readonly Expression<Func<TaskTemplate, TaskTemplateDto>> FromTaskTemplate =
      t => new TaskTemplateDto(
        t.Id,
        t.FamilyId,
        t.Title,
        t.Points,
        t.Schedule.Type,
        t.Schedule.Time,
        t.Schedule.DayOfWeek,
        t.Schedule.DayOfMonth,
        t.PetId,
        t.Pet.Name,
        t.CreatedAt,
        t.DueDuration);
  }
}

public record GetTaskTemplatesByFamilyQuery(Guid FamilyId, bool? IsActive = null)
  : IQuery<Result<List<TaskTemplateDto>>>;

public class GetTaskTemplatesByFamilyHandler(
  IReadRepository<TaskTemplate> templateRepository)
  : IQueryHandler<GetTaskTemplatesByFamilyQuery, Result<List<TaskTemplateDto>>>
{
  public async ValueTask<Result<List<TaskTemplateDto>>> Handle(GetTaskTemplatesByFamilyQuery request,
    CancellationToken cancellationToken)
  {
    var spec = new TaskTemplatesDtoByFamilyIdsSpec(request.FamilyId);
    var templates = await templateRepository.ListAsync(spec, cancellationToken);
    return Result<List<TaskTemplateDto>>.Success(templates);
  }
}
