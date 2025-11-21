namespace FamilyTaskManager.UseCases.Pets;

/// <summary>
/// Command to calculate mood scores for all pets
/// </summary>
public record CalculateAllPetsMoodCommand : ICommand<Result>;

public class CalculateAllPetsMoodHandler(IMediator mediator) 
  : ICommandHandler<CalculateAllPetsMoodCommand, Result>
{
  public async ValueTask<Result> Handle(CalculateAllPetsMoodCommand command, CancellationToken cancellationToken)
  {
    // Get all pets
    var petsResult = await mediator.Send(new GetAllPetsQuery(), cancellationToken);
    
    if (!petsResult.IsSuccess)
    {
      return Result.Error("Failed to retrieve pets");
    }

    var pets = petsResult.Value;
    
    // Calculate mood for each pet (will register domain events if needed)
    foreach (var pet in pets)
    {
      await mediator.Send(new CalculatePetMoodScoreCommand(pet.Id), cancellationToken);
    }

    return Result.Success();
  }
}
