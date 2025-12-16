namespace FamilyTaskManager.UseCases.Features.TaskTemplatesManagement.Commands;

public record ClearTaskTemplateResponsibleCommand(Guid TaskTemplateId)
  : ICommand<Result>;

public class ClearTaskTemplateResponsibleHandler(
  IAppRepository<TaskTemplate> taskTemplateAppRepository)
  : ICommandHandler<ClearTaskTemplateResponsibleCommand, Result>
{
  public async ValueTask<Result> Handle(ClearTaskTemplateResponsibleCommand command, CancellationToken cancellationToken)
  {
    var taskTemplate = await taskTemplateAppRepository.GetByIdAsync(command.TaskTemplateId, cancellationToken);
    if (taskTemplate == null) return Result.NotFound("Task template not found");

    taskTemplate.ClearAllResponsible();

    await taskTemplateAppRepository.UpdateAsync(taskTemplate, cancellationToken);
    await taskTemplateAppRepository.SaveChangesAsync(cancellationToken);

    return Result.Success();
  }
}