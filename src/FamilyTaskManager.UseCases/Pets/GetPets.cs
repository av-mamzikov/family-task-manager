namespace FamilyTaskManager.UseCases.Pets;

public record GetPetsQuery(Guid FamilyId) : IQuery<Result<List<PetDto>>>;

public class GetPetsHandler(IAppRepository<Pet> petAppRepository)
  : IQueryHandler<GetPetsQuery, Result<List<PetDto>>>
{
  public async ValueTask<Result<List<PetDto>>> Handle(GetPetsQuery query, CancellationToken cancellationToken)
  {
    var spec = new GetPetsByFamilySpec(query.FamilyId);
    var pets = await petAppRepository.ListAsync(spec, cancellationToken);

    var result = pets.Select(p => new PetDto(p.Id, p.FamilyId, p.Name, p.Type, p.MoodScore)).ToList();

    return Result<List<PetDto>>.Success(result);
  }
}
