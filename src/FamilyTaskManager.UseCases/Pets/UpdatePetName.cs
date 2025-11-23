namespace FamilyTaskManager.UseCases.Pets;

public record UpdatePetNameCommand(Guid PetId, string NewName) : ICommand<Result>;

public class UpdatePetNameHandler(IRepository<Pet> petRepository)
  : ICommandHandler<UpdatePetNameCommand, Result>
{
  public async ValueTask<Result> Handle(UpdatePetNameCommand command, CancellationToken cancellationToken)
  {
    var pet = await petRepository.GetByIdAsync(command.PetId, cancellationToken);
    if (pet == null)
    {
      return Result.NotFound("Pet not found");
    }

    if (string.IsNullOrWhiteSpace(command.NewName) || command.NewName.Length < 2 || command.NewName.Length > 50)
    {
      return Result.Invalid(new ValidationError("Pet name must be between 2 and 50 characters"));
    }

    pet.UpdateName(command.NewName);
    await petRepository.UpdateAsync(pet, cancellationToken);

    return Result.Success();
  }
}
