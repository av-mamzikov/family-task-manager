using FamilyTaskManager.UseCases.Features.SpotManagement.Dtos;

namespace FamilyTaskManager.UseCases.Features.SpotManagement.Queries;

public record GetSpotByIdQuery(Guid SpotId) : IQuery<Result<SpotDto>>;

public class GetSpotByIdHandler(IAppRepository<Spot> spotAppRepository)
  : IQueryHandler<GetSpotByIdQuery, Result<SpotDto>>
{
  public async ValueTask<Result<SpotDto>> Handle(GetSpotByIdQuery query, CancellationToken cancellationToken)
  {
    var spot = await spotAppRepository.GetByIdAsync(query.SpotId, cancellationToken);

    if (spot == null)
      return Result<SpotDto>.NotFound();

    var result = new SpotDto(spot.Id, spot.FamilyId, spot.Name, spot.Type, spot.MoodScore);

    return Result<SpotDto>.Success(result);
  }
}
