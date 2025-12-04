namespace FamilyTaskManager.UseCases.Spots;

public record GetSpotsQuery(Guid FamilyId) : IQuery<Result<List<SpotDto>>>;

public class GetSpotsHandler(IAppRepository<Spot> spotAppRepository)
  : IQueryHandler<GetSpotsQuery, Result<List<SpotDto>>>
{
  public async ValueTask<Result<List<SpotDto>>> Handle(GetSpotsQuery query, CancellationToken cancellationToken)
  {
    var spec = new GetSpotsByFamilySpec(query.FamilyId);
    var spots = await spotAppRepository.ListAsync(spec, cancellationToken);

    var result = spots.Select(p => new SpotDto(p.Id, p.FamilyId, p.Name, p.Type, p.MoodScore)).ToList();

    return Result<List<SpotDto>>.Success(result);
  }
}
