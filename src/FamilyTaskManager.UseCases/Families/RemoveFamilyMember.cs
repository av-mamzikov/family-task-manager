namespace FamilyTaskManager.UseCases.Families;

public record RemoveFamilyMemberCommand(Guid FamilyId, Guid TargetUserId, Guid RequestedBy)
  : ICommand<Result>;

public class RemoveFamilyMemberHandler(IRepository<Family> familyRepository)
  : ICommandHandler<RemoveFamilyMemberCommand, Result>
{
  public async ValueTask<Result> Handle(RemoveFamilyMemberCommand command, CancellationToken cancellationToken)
  {
    var spec = new GetFamilyWithMembersSpec(command.FamilyId);
    var family = await familyRepository.FirstOrDefaultAsync(spec, cancellationToken);
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
      return Result.Forbidden("Только администратор может удалять участников");
    }

    var member = family.Members.FirstOrDefault(m => m.UserId == command.TargetUserId && m.IsActive);
    if (member == null)
    {
      return Result.NotFound("Участник не найден");
    }

    if (member.UserId == command.RequestedBy)
    {
      return Result.Forbidden("Нельзя удалить самого себя");
    }

    member.Deactivate();
    await familyRepository.UpdateAsync(family, cancellationToken);

    return Result.Success();
  }
}
