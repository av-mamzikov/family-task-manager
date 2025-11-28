namespace FamilyTaskManager.UseCases.Pets;

public record DeletePetCommand(Guid PetId, Guid UserId) : ICommand<Result>;

public class DeletePetHandler(
  IRepository<Pet> petRepository,
  IRepository<User> userRepository,
  IRepository<Family> familyRepository) : ICommandHandler<DeletePetCommand, Result>
{
  public async ValueTask<Result> Handle(DeletePetCommand command, CancellationToken cancellationToken)
  {
    // Verify user exists
    var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);
    if (user == null)
    {
      return Result.NotFound("User not found");
    }

    // Get pet
    var pet = await petRepository.GetByIdAsync(command.PetId, cancellationToken);
    if (pet == null)
    {
      return Result.NotFound("Pet not found");
    }

    // Get family with members to check permissions
    var spec = new GetFamilyWithMembersSpec(pet.FamilyId);
    var family = await familyRepository.FirstOrDefaultAsync(spec, cancellationToken);
    if (family == null)
    {
      return Result.NotFound("Family not found");
    }

    // Check if user is admin of this family
    var adminMember = family.Members.FirstOrDefault(m => m.UserId == command.UserId && m.Role == FamilyRole.Admin);
    if (adminMember == null)
    {
      return Result.Forbidden("Only family administrators can delete pets");
    }

    // Mark pet for deletion (this will register the deletion event)
    pet.MarkForDeletion();

    // Delete the pet (this will cascade delete related entities like task templates and tasks)
    await petRepository.DeleteAsync(pet, cancellationToken);
    await petRepository.SaveChangesAsync(cancellationToken);

    return Result.Success();
  }
}
