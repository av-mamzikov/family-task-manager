using FamilyTaskManager.UseCases.Features.SpotManagement.Dtos;

namespace FamilyTaskManager.UseCases.Features.SpotManagement.Queries;

public record GetAllSpotsQuery : IQuery<Result<List<SpotDto>>>;

public class GetAllSpotsHandler(IAppRepository<Spot> appRepository)
  : IQueryHandler<GetAllSpotsQuery, Result<List<SpotDto>>>
{
  public async ValueTask<Result<List<SpotDto>>> Handle(GetAllSpotsQuery request, CancellationToken cancellationToken)
  {
    var spots = await appRepository.ListAsync(cancellationToken);

    var result = spots.Select(p => new SpotDto(p.Id, p.FamilyId, p.Name, p.Type, p.MoodScore)).ToList();

    return Result.Success(result);
  }
}
