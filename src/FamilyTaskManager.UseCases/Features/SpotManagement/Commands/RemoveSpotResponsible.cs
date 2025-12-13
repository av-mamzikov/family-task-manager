namespace FamilyTaskManager.UseCases.Features.SpotManagement.Commands;

public record RemoveSpotResponsibleCommand(Guid SpotId, Guid FamilyMemberId)
  : ICommand<Result>;

public class RemoveSpotResponsibleHandler(
  IAppRepository<Spot> spotAppRepository,
  IReadOnlyEntityRepository<FamilyMember> familyMemberRepository)
  : ICommandHandler<RemoveSpotResponsibleCommand, Result>
{
  public async ValueTask<Result> Handle(RemoveSpotResponsibleCommand command, CancellationToken cancellationToken)
  {
    var spot = await spotAppRepository.GetByIdAsync(command.SpotId, cancellationToken);
    if (spot == null) return Result.NotFound("Spot not found");

    var member = await familyMemberRepository.GetByIdAsync(command.FamilyMemberId, cancellationToken);
    if (member == null) return Result.NotFound("Family member not found");

    spot.RemoveResponsible(member);

    await spotAppRepository.UpdateAsync(spot, cancellationToken);
    await spotAppRepository.SaveChangesAsync(cancellationToken);

    return Result.Success();
  }
}
