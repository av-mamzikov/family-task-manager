namespace FamilyTaskManager.UseCases.TaskTemplates;

public record DeactivateTaskTemplateCommand(Guid TemplateId, Guid FamilyId) : ICommand<Result>;

public class DeactivateTaskTemplateHandler(IRepository<TaskTemplate> templateRepository)
  : ICommandHandler<DeactivateTaskTemplateCommand, Result>
{
  public async ValueTask<Result> Handle(DeactivateTaskTemplateCommand command, CancellationToken cancellationToken)
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

    if (!template.IsActive)
    {
      return Result.Error("Template is already inactive");
    }

    template.Deactivate();
    await templateRepository.UpdateAsync(template, cancellationToken);

    return Result.Success();
  }
}
