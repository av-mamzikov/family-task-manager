namespace FamilyTaskManager.UseCases.Families;

public record DeleteFamilyCommand(Guid FamilyId, Guid UserId) : ICommand<Result>;

public class DeleteFamilyHandler(
  IRepository<Family> familyRepository,
  IRepository<User> userRepository) : ICommandHandler<DeleteFamilyCommand, Result>
{
  public async ValueTask<Result> Handle(DeleteFamilyCommand command, CancellationToken cancellationToken)
  {
    // Verify user exists
    var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);
    if (user == null)
    {
      return Result.NotFound("User not found");
    }

    // Get family with members using specification
    var spec = new GetFamilyWithMembersSpec(command.FamilyId);
    var family = await familyRepository.FirstOrDefaultAsync(spec, cancellationToken);
    if (family == null)
    {
      return Result.NotFound("Family not found");
    }

    // Check if user is admin of this family
    var adminMember = family.Members.FirstOrDefault(m => m.UserId == command.UserId && m.Role == FamilyRole.Admin);
    if (adminMember == null)
    {
      return Result.Forbidden("Only family administrators can delete the family");
    }

    // Delete the family (this will cascade delete related entities)
    await familyRepository.DeleteAsync(family, cancellationToken);

    return Result.Success();
  }
}
