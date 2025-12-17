namespace FamilyTaskManager.UseCases.Features.FamilyManagement.Commands;

public record DeleteFamilyCommand(Guid FamilyId, Guid UserId) : ICommand<Result>;

public class DeleteFamilyHandler(
  IAppRepository<Family> familyAppRepository,
  IAppRepository<User> userAppRepository) : ICommandHandler<DeleteFamilyCommand, Result>
{
  public async ValueTask<Result> Handle(DeleteFamilyCommand command, CancellationToken cancellationToken)
  {
    // Verify user exists
    var user = await userAppRepository.GetByIdAsync(command.UserId, cancellationToken);
    if (user == null) return Result.NotFound("User not found");

    var family = await familyAppRepository.GetByIdAsync(command.FamilyId, cancellationToken);
    if (family == null) return Result.NotFound("Family not found");

    // Check if user is admin of this family
    var adminMember = family.Members.FirstOrDefault(m => m.UserId == command.UserId && m.Role == FamilyRole.Admin);
    if (adminMember == null) return Result.Forbidden("Only family administrators can delete the family");

    // Delete the family (this will cascade delete related entities)
    await familyAppRepository.DeleteAsync(family, cancellationToken);

    return Result.Success();
  }
}
