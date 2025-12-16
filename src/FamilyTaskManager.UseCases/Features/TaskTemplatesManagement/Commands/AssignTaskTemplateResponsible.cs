namespace FamilyTaskManager.UseCases.Features.TaskTemplatesManagement.Commands;

public record AssignTaskTemplateResponsibleCommand(Guid TaskTemplateId, Guid FamilyMemberId)
  : ICommand<Result>;

public class AssignTaskTemplateResponsibleHandler(
  IAppRepository<TaskTemplate> taskTemplateAppRepository,
  IReadOnlyEntityRepository<FamilyMember> familyMemberRepository)
  : ICommandHandler<AssignTaskTemplateResponsibleCommand, Result>
{
  public async ValueTask<Result> Handle(AssignTaskTemplateResponsibleCommand command, CancellationToken cancellationToken)
  {
    var taskTemplate = await taskTemplateAppRepository.GetByIdAsync(command.TaskTemplateId, cancellationToken);
    if (taskTemplate == null) return Result.NotFound("Task template not found");

    var member = await familyMemberRepository.GetByIdAsync(command.FamilyMemberId, cancellationToken);
    if (member == null) return Result.NotFound("Family member not found");

    // Domain method enforces invariants: same family, IsActive, no duplicates
    taskTemplate.AssignResponsible(member);

    await taskTemplateAppRepository.UpdateAsync(taskTemplate, cancellationToken);
    await taskTemplateAppRepository.SaveChangesAsync(cancellationToken);

    return Result.Success();
  }
}