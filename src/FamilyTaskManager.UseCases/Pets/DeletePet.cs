namespace FamilyTaskManager.UseCases.Pets;

public record DeletePetCommand(Guid PetId, Guid UserId) : ICommand<Result>;

public class DeletePetHandler(
  IAppRepository<Pet> petAppRepository,
  IAppRepository<User> userAppRepository,
  IAppRepository<Family> familyAppRepository) : ICommandHandler<DeletePetCommand, Result>
{
  public async ValueTask<Result> Handle(DeletePetCommand command, CancellationToken cancellationToken)
  {
    // Verify user exists
    var user = await userAppRepository.GetByIdAsync(command.UserId, cancellationToken);
    if (user == null)
    {
      return Result.NotFound("User not found");
    }

    // Get pet
    var pet = await petAppRepository.GetByIdAsync(command.PetId, cancellationToken);
    if (pet == null)
    {
      return Result.NotFound("Pet not found");
    }

    // Get family with members to check permissions
    var spec = new GetFamilyWithMembersSpec(pet.FamilyId);
    var family = await familyAppRepository.FirstOrDefaultAsync(spec, cancellationToken);
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

    // Soft delete the pet (registers the deletion event and keeps data)
    pet.SoftDelete();

    await petAppRepository.UpdateAsync(pet, cancellationToken);
    await petAppRepository.SaveChangesAsync(cancellationToken);

    return Result.Success();
  }
}
