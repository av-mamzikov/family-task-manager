namespace FamilyTaskManager.UseCases.Statistics;

public record LeaderboardEntryDto(Guid UserId, string UserName, int Points, FamilyRole Role);

public record GetLeaderboardQuery(Guid FamilyId) : IQuery<Result<List<LeaderboardEntryDto>>>;

public class GetLeaderboardHandler(
  IRepository<Family> familyRepository,
  IRepository<User> userRepository) : IQueryHandler<GetLeaderboardQuery, Result<List<LeaderboardEntryDto>>>
{
  public async ValueTask<Result<List<LeaderboardEntryDto>>> Handle(GetLeaderboardQuery query, CancellationToken cancellationToken)
  {
    // Get family with members
    var spec = new GetFamilyWithMembersSpec(query.FamilyId);
    var family = await familyRepository.FirstOrDefaultAsync(spec, cancellationToken);
    
    if (family == null)
    {
      return Result<List<LeaderboardEntryDto>>.NotFound("Family not found");
    }

    if (!family.LeaderboardEnabled)
    {
      return Result<List<LeaderboardEntryDto>>.Error("Leaderboard is disabled for this family");
    }

    // Get active members sorted by points
    var activeMembers = family.Members
      .Where(m => m.IsActive)
      .OrderByDescending(m => m.Points)
      .ToList();

    // Get user names
    var userIds = activeMembers.Select(m => m.UserId).ToList();
    var userSpec = new GetUsersByIdsSpec(userIds);
    var users = await userRepository.ListAsync(userSpec, cancellationToken);
    var userDict = users.ToDictionary(u => u.Id, u => u.Name);

    // Build result
    var result = activeMembers.Select(m => new LeaderboardEntryDto(
      m.UserId,
      userDict.GetValueOrDefault(m.UserId, "Unknown"),
      m.Points,
      m.Role
    )).ToList();

    return Result<List<LeaderboardEntryDto>>.Success(result);
  }
}
