namespace FamilyTaskManager.UseCases.TaskTemplates;

public record DeleteTaskTemplateCommand(Guid TemplateId, Guid FamilyId) : ICommand<Result>;

public class DeleteTaskTemplateHandler(IRepository<TaskTemplate> templateRepository)
  : ICommandHandler<DeleteTaskTemplateCommand, Result>
{
  public async ValueTask<Result> Handle(DeleteTaskTemplateCommand command, CancellationToken cancellationToken)
  {
    var template = await templateRepository.GetByIdAsync(command.TemplateId, cancellationToken);
    if (template == null)
    {
      return Result.NotFound("Task template not found");
    }

    // Authorization check - ensure template belongs to the requested family
    if (template.FamilyId != command.FamilyId)
    {
      return Result.NotFound("Task template not found");
    }

    await templateRepository.DeleteAsync(template, cancellationToken);

    return Result.Success();
  }
}
