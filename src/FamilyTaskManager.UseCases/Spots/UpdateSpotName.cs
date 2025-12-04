namespace FamilyTaskManager.UseCases.Spots;

public record UpdateSpotNameCommand(Guid SpotId, string NewName) : ICommand<Result>;

public class UpdateSpotNameHandler(IAppRepository<Spot> spotAppRepository)
  : ICommandHandler<UpdateSpotNameCommand, Result>
{
  public async ValueTask<Result> Handle(UpdateSpotNameCommand command, CancellationToken cancellationToken)
  {
    var spot = await spotAppRepository.GetByIdAsync(command.SpotId, cancellationToken);
    if (spot == null)
    {
      return Result.NotFound("Спот не найден");
    }

    if (string.IsNullOrWhiteSpace(command.NewName) || command.NewName.Length < 2 || command.NewName.Length > 50)
    {
      return Result.Invalid(new ValidationError("Имя спота должно быть длиной от 2 до 50 символов"));
    }

    spot.UpdateName(command.NewName);
    await spotAppRepository.UpdateAsync(spot, cancellationToken);

    return Result.Success();
  }
}
