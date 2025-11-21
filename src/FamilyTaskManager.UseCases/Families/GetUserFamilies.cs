namespace FamilyTaskManager.UseCases.Families;

public record FamilyDto(Guid Id, string Name, string Timezone, bool LeaderboardEnabled, FamilyRole UserRole, int UserPoints);

public record GetUserFamiliesQuery(Guid UserId) : IQuery<Result<List<FamilyDto>>>;

public class GetUserFamiliesHandler(IRepository<Family> familyRepository) 
  : IQueryHandler<GetUserFamiliesQuery, Result<List<FamilyDto>>>
{
  public async ValueTask<Result<List<FamilyDto>>> Handle(GetUserFamiliesQuery query, CancellationToken cancellationToken)
  {
    var spec = new GetFamiliesByUserIdSpec(query.UserId);
    var families = await familyRepository.ListAsync(spec, cancellationToken);

    var result = families.Select(f =>
    {
      var member = f.Members.First(m => m.UserId == query.UserId && m.IsActive);
      return new FamilyDto(f.Id, f.Name, f.Timezone, f.LeaderboardEnabled, member.Role, member.Points);
    }).ToList();

    return Result<List<FamilyDto>>.Success(result);
  }
}
