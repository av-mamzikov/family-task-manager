namespace FamilyTaskManager.UseCases.Statistics;

public record ActionHistoryDto(
  Guid Id,
  string ActionType,
  string Description,
  DateTime CreatedAt,
  Guid UserId,
  string UserName);

public record GetActionHistoryQuery(
  Guid FamilyId,
  Guid? UserId = null,
  int? DaysBack = null,
  int Limit = 100) : IQuery<Result<List<ActionHistoryDto>>>;

public class GetActionHistoryHandler(
  IRepository<ActionHistory> historyRepository,
  IRepository<User> userRepository) : IQueryHandler<GetActionHistoryQuery, Result<List<ActionHistoryDto>>>
{
  public async ValueTask<Result<List<ActionHistoryDto>>> Handle(GetActionHistoryQuery query, CancellationToken cancellationToken)
  {
    var spec = new GetActionHistorySpec(query.FamilyId, query.UserId, query.DaysBack, query.Limit);
    var history = await historyRepository.ListAsync(spec, cancellationToken);

    // Get unique user IDs
    var userIds = history
      .Select(h => h.UserId)
      .Distinct()
      .ToList();

    // Get user names
    Dictionary<Guid, string> userDict = new();
    if (userIds.Any())
    {
      var userSpec = new GetUsersByIdsSpec(userIds);
      var users = await userRepository.ListAsync(userSpec, cancellationToken);
      userDict = users.ToDictionary(u => u.Id, u => u.Name);
    }

    // Build result
    var result = history.Select(h => new ActionHistoryDto(
      h.Id,
      h.ActionType.ToString(),
      h.Description,
      h.CreatedAt,
      h.UserId,
      userDict.GetValueOrDefault(h.UserId, "Unknown")
    )).ToList();

    return Result<List<ActionHistoryDto>>.Success(result);
  }
}
