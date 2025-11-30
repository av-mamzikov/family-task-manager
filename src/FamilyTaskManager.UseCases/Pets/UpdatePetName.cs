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
      return Result.NotFound("Питомец не найден");
    }

    if (string.IsNullOrWhiteSpace(command.NewName) || command.NewName.Length < 2 || command.NewName.Length > 50)
    {
      return Result.Invalid(new ValidationError("Имя питомца должно быть длиной от 2 до 50 символов"));
    }

    pet.UpdateName(command.NewName);
    await petRepository.UpdateAsync(pet, cancellationToken);

    return Result.Success();
  }
}
