namespace FamilyTaskManager.UseCases.Features.TaskTemplatesManagement.Commands;

public record RemoveTaskTemplateResponsibleCommand(Guid TaskTemplateId, Guid FamilyMemberId)
  : ICommand<Result>;

public class RemoveTaskTemplateResponsibleHandler(
  IAppRepository<TaskTemplate> taskTemplateAppRepository,
  IReadOnlyEntityRepository<FamilyMember> familyMemberRepository)
  : ICommandHandler<RemoveTaskTemplateResponsibleCommand, Result>
{
  public async ValueTask<Result> Handle(RemoveTaskTemplateResponsibleCommand command, CancellationToken cancellationToken)
  {
    var taskTemplate = await taskTemplateAppRepository.GetByIdAsync(command.TaskTemplateId, cancellationToken);
    if (taskTemplate == null) return Result.NotFound("Task template not found");

    var member = await familyMemberRepository.GetByIdAsync(command.FamilyMemberId, cancellationToken);
    if (member == null) return Result.NotFound("Family member not found");

    taskTemplate.RemoveResponsible(member);

    await taskTemplateAppRepository.UpdateAsync(taskTemplate, cancellationToken);
    await taskTemplateAppRepository.SaveChangesAsync(cancellationToken);

    return Result.Success();
  }
}