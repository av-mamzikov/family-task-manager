using FamilyTaskManager.Core.Services;

namespace FamilyTaskManager.UseCases.Pets;

public record PetMoodScoreResult(int OldMoodScore, int NewMoodScore, bool HasChanged);

public record CalculatePetMoodScoreCommand(Guid PetId) : ICommand<Result<PetMoodScoreResult>>;

public class CalculatePetMoodScoreHandler(
  IRepository<Pet> petRepository,
  IPetMoodCalculator moodCalculator)
  : ICommandHandler<CalculatePetMoodScoreCommand, Result<PetMoodScoreResult>>
{
  public async ValueTask<Result<PetMoodScoreResult>> Handle(CalculatePetMoodScoreCommand request,
    CancellationToken cancellationToken)
  {
    var pet = await petRepository.GetByIdAsync(request.PetId, cancellationToken);
    if (pet == null)
    {
      return Result.NotFound($"Pet with ID {request.PetId} not found.");
    }

    var oldMoodScore = pet.MoodScore;
    var newMoodScore = await moodCalculator.CalculateMoodScoreAsync(request.PetId, cancellationToken);

    pet.UpdateMoodScore(newMoodScore);
    await petRepository.SaveChangesAsync(cancellationToken);

    return Result.Success(new PetMoodScoreResult(oldMoodScore, newMoodScore, oldMoodScore != newMoodScore));
  }
}
