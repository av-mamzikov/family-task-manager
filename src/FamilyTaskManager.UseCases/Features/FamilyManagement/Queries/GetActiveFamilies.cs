namespace FamilyTaskManager.UseCases.Features.FamilyManagement.Queries;

public record GetActiveFamiliesQuery : IQuery<Result<List<Family>>>;

public class GetActiveFamiliesHandler(IAppRepository<Family> familyAppRepository)
  : IQueryHandler<GetActiveFamiliesQuery, Result<List<Family>>>
{
  public async ValueTask<Result<List<Family>>> Handle(GetActiveFamiliesQuery query, CancellationToken cancellationToken)
  {
    // Get all families - in this context, we consider all families as potentially active
    // In a more complex implementation, you might filter by last activity or other criteria
    var families = await familyAppRepository.ListAsync(cancellationToken);
    return Result.Success(families.ToList());
  }
}
