using FamilyTaskManager.Core.Services;

namespace FamilyTaskManager.UseCases.Tasks;

public record CreateTaskInstanceFromTemplateCommand(Guid TemplateId, DateTime DueAt) : ICommand<Result<Guid>>;

public class CreateTaskInstanceFromTemplateHandler(
  IRepository<TaskTemplate> templateRepository,
  IRepository<TaskInstance> taskRepository,
  ITaskInstanceFactory taskInstanceFactory)
  : ICommandHandler<CreateTaskInstanceFromTemplateCommand, Result<Guid>>
{
  public async ValueTask<Result<Guid>> Handle(CreateTaskInstanceFromTemplateCommand request,
    CancellationToken cancellationToken)
  {
    var template = await templateRepository.GetByIdAsync(request.TemplateId, cancellationToken);
    if (template == null)
      return Result.NotFound($"TaskTemplate with ID {request.TemplateId} not found.");

    var spec = new TaskInstancesByTemplateSpec(request.TemplateId);
    var existingInstances = await taskRepository.ListAsync(spec, cancellationToken);
    var createResult = taskInstanceFactory.CreateFromTemplate(template, request.DueAt, existingInstances);
    if (!createResult.IsSuccess)
      return Result.Error(string.Join(", ", createResult.Errors));

    await taskRepository.AddAsync(createResult.Value, cancellationToken);
    await taskRepository.SaveChangesAsync(cancellationToken);

    return Result.Success(createResult.Value.Id);
  }
}
