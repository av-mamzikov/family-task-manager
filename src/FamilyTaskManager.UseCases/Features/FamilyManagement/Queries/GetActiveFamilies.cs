using FamilyTaskManager.Core.FamilyAggregate.Specifications;

namespace FamilyTaskManager.UseCases.Features.FamilyManagement.Queries;

public record GetActiveFamiliesQuery : IQuery<Result<List<Family>>>;

public class GetActiveFamiliesHandler(IAppRepository<Family> familyAppRepository)
  : IQueryHandler<GetActiveFamiliesQuery, Result<List<Family>>>
{
  public async ValueTask<Result<List<Family>>> Handle(GetActiveFamiliesQuery query, CancellationToken cancellationToken)
  {
    var families = await familyAppRepository.ListAsync(new ActiveFamiliesSpec(), cancellationToken);
    return Result.Success(families.ToList());
  }
}
