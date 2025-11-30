namespace FamilyTaskManager.UseCases.TaskTemplates;

public record GetActiveTaskTemplatesQuery : IQuery<Result<List<TaskTemplate>>>;

public class GetActiveTaskTemplatesHandler(IRepository<TaskTemplate> repository)
  : IQueryHandler<GetActiveTaskTemplatesQuery, Result<List<TaskTemplate>>>
{
  public async ValueTask<Result<List<TaskTemplate>>> Handle(GetActiveTaskTemplatesQuery request,
    CancellationToken cancellationToken)
  {
    var templates = await repository.ListAsync(cancellationToken);
    return Result.Success(templates);
  }
}
