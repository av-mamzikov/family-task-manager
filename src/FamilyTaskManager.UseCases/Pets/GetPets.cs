namespace FamilyTaskManager.UseCases.Pets;

public record PetDto(Guid Id, string Name, PetType Type, int MoodScore);

public record GetPetsQuery(Guid FamilyId) : IQuery<Result<List<PetDto>>>;

public class GetPetsHandler(IRepository<Pet> petRepository) 
  : IQueryHandler<GetPetsQuery, Result<List<PetDto>>>
{
  public async ValueTask<Result<List<PetDto>>> Handle(GetPetsQuery query, CancellationToken cancellationToken)
  {
    var spec = new GetPetsByFamilySpec(query.FamilyId);
    var pets = await petRepository.ListAsync(spec, cancellationToken);

    var result = pets.Select(p => new PetDto(p.Id, p.Name, p.Type, p.MoodScore)).ToList();

    return Result<List<PetDto>>.Success(result);
  }
}
