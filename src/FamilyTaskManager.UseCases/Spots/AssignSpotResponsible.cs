namespace FamilyTaskManager.UseCases.Spots;

public record AssignSpotResponsibleCommand(Guid SpotId, Guid FamilyMemberId)
  : ICommand<Result>;

public class AssignSpotResponsibleHandler(
  IAppRepository<Spot> spotAppRepository,
  IReadOnlyEntityRepository<FamilyMember> familyMemberRepository)
  : ICommandHandler<AssignSpotResponsibleCommand, Result>
{
  public async ValueTask<Result> Handle(AssignSpotResponsibleCommand command, CancellationToken cancellationToken)
  {
    var spot = await spotAppRepository.GetByIdAsync(command.SpotId, cancellationToken);
    if (spot == null) return Result.NotFound("Spot not found");

    var member = await familyMemberRepository.GetByIdAsync(command.FamilyMemberId, cancellationToken);
    if (member == null) return Result.NotFound("Family member not found");

    // Domain method enforces invariants: same family, IsActive, no duplicates
    spot.AssignResponsible(member);

    await spotAppRepository.UpdateAsync(spot, cancellationToken);
    await spotAppRepository.SaveChangesAsync(cancellationToken);

    return Result.Success();
  }
}
