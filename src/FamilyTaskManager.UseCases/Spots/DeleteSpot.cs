namespace FamilyTaskManager.UseCases.Spots;

public record DeleteSpotCommand(Guid SpotId, Guid UserId) : ICommand<Result>;

public class DeleteSpotHandler(
  IAppRepository<Spot> spotAppRepository,
  IAppRepository<User> userAppRepository,
  IAppRepository<Family> familyAppRepository) : ICommandHandler<DeleteSpotCommand, Result>
{
  public async ValueTask<Result> Handle(DeleteSpotCommand command, CancellationToken cancellationToken)
  {
    // Verify user exists
    var user = await userAppRepository.GetByIdAsync(command.UserId, cancellationToken);
    if (user == null)
    {
      return Result.NotFound("User not found");
    }

    // Get Spot
    var spot = await spotAppRepository.GetByIdAsync(command.SpotId, cancellationToken);
    if (spot == null)
    {
      return Result.NotFound("Spot not found");
    }

    // Get family with members to check permissions
    var spec = new GetFamilyWithMembersSpec(spot.FamilyId);
    var family = await familyAppRepository.FirstOrDefaultAsync(spec, cancellationToken);
    if (family == null)
    {
      return Result.NotFound("Family not found");
    }

    // Check if user is admin of this family
    var adminMember = family.Members.FirstOrDefault(m => m.UserId == command.UserId && m.Role == FamilyRole.Admin);
    if (adminMember == null)
    {
      return Result.Forbidden("Only family administrators can delete Spots");
    }

    // Soft delete the Spot (registers the deletion event and keeps data)
    spot.SoftDelete();

    await spotAppRepository.UpdateAsync(spot, cancellationToken);
    await spotAppRepository.SaveChangesAsync(cancellationToken);

    return Result.Success();
  }
}
