using FamilyTaskManager.Core.FamilyAggregate.Specifications;

namespace FamilyTaskManager.UseCases.Features.FamilyManagement.Commands;

public record RemoveFamilyMemberCommand(Guid FamilyId, Guid MemberId, Guid RequestedBy)
  : ICommand<Result>;

public class RemoveFamilyMemberHandler(IAppRepository<Family> familyAppRepository)
  : ICommandHandler<RemoveFamilyMemberCommand, Result>
{
  public async ValueTask<Result> Handle(RemoveFamilyMemberCommand command, CancellationToken cancellationToken)
  {
    var spec = new GetFamilyWithMembersSpec(command.FamilyId);
    var family = await familyAppRepository.FirstOrDefaultAsync(spec, cancellationToken);
    if (family == null) return Result.NotFound("Семья не найдена");

    var requester = family.Members.FirstOrDefault(m => m.UserId == command.RequestedBy && m.IsActive);
    if (requester == null) return Result.Forbidden("Пользователь не входит в эту семью");

    if (requester.Role != FamilyRole.Admin) return Result.Forbidden("Только администратор может удалять участников");

    var member = family.Members.FirstOrDefault(m => m.Id == command.MemberId && m.IsActive);
    if (member == null) return Result.NotFound("Участник не найден");

    if (member.UserId == command.RequestedBy) return Result.Forbidden("Нельзя удалить самого себя");

    member.Deactivate();
    await familyAppRepository.UpdateAsync(family, cancellationToken);

    return Result.Success();
  }
}
