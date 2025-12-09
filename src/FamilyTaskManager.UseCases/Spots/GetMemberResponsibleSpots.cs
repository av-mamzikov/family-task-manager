using FamilyTaskManager.UseCases.Contracts;

namespace FamilyTaskManager.UseCases.Spots;

public record GetMemberResponsibleSpotsQuery(Guid FamilyMemberId)
  : IQuery<Result<List<SpotDto>>>;

public class GetMemberResponsibleSpotsHandler(IAppRepository<Spot> spotAppRepository)
  : IQueryHandler<GetMemberResponsibleSpotsQuery, Result<List<SpotDto>>>
{
  public async ValueTask<Result<List<SpotDto>>> Handle(
    GetMemberResponsibleSpotsQuery query,
    CancellationToken cancellationToken)
  {
    var spec = new GetSpotsByResponsibleMemberSpec(query.FamilyMemberId);
    var spots = await spotAppRepository.ListAsync(spec, cancellationToken);

    var result = spots
      .Select(p => new SpotDto(p.Id, p.FamilyId, p.Name, p.Type, p.MoodScore))
      .ToList();

    return Result<List<SpotDto>>.Success(result);
  }
}
