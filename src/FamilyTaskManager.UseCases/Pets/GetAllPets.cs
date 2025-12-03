namespace FamilyTaskManager.UseCases.Pets;

public record GetAllPetsQuery : IQuery<Result<List<PetDto>>>;

public class GetAllPetsHandler(IAppRepository<Pet> appRepository)
  : IQueryHandler<GetAllPetsQuery, Result<List<PetDto>>>
{
  public async ValueTask<Result<List<PetDto>>> Handle(GetAllPetsQuery request, CancellationToken cancellationToken)
  {
    var pets = await appRepository.ListAsync(cancellationToken);

    var result = pets.Select(p => new PetDto(p.Id, p.FamilyId, p.Name, p.Type, p.MoodScore)).ToList();

    return Result.Success(result);
  }
}
