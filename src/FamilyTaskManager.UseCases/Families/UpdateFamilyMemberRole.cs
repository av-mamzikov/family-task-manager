namespace FamilyTaskManager.UseCases.Families;

public record UpdateFamilyMemberRoleCommand(
  Guid FamilyId,
  Guid memberId,
  Guid RequestedBy,
  FamilyRole NewRole) : ICommand<Result>;

public class UpdateFamilyMemberRoleHandler(IAppRepository<Family> familyAppRepository)
  : ICommandHandler<UpdateFamilyMemberRoleCommand, Result>
{
  public async ValueTask<Result> Handle(UpdateFamilyMemberRoleCommand command, CancellationToken cancellationToken)
  {
    var spec = new GetFamilyWithMembersSpec(command.FamilyId);
    var family = await familyAppRepository.FirstOrDefaultAsync(spec, cancellationToken);
    if (family == null)
    {
      return Result.NotFound("Семья не найдена");
    }

    var requester = family.Members.FirstOrDefault(m => m.UserId == command.RequestedBy && m.IsActive);
    if (requester == null)
    {
      return Result.Forbidden("Пользователь не входит в эту семью");
    }

    if (requester.Role != FamilyRole.Admin)
    {
      return Result.Forbidden("Только администратор может менять роли участников");
    }

    var member = family.Members.FirstOrDefault(m => m.Id == command.memberId && m.IsActive);
    if (member == null)
    {
      return Result.NotFound("Участник не найден");
    }

    if (member.UserId == command.RequestedBy)
    {
      return Result.Forbidden("Нельзя изменять собственную роль");
    }

    if (member.Role == command.NewRole)
    {
      return Result.Invalid(new ValidationError("У участника уже установлена эта роль"));
    }

    member.UpdateRole(command.NewRole);
    await familyAppRepository.UpdateAsync(family, cancellationToken);

    return Result.Success();
  }
}
