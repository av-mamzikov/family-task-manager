using FamilyTaskManager.Core.TaskAggregate.Specifications;

namespace FamilyTaskManager.UseCases.Features.TasksManagement.Queries;

public record NextTaskExecutorResult(
  Guid FamilyMemberId,
  Guid UserId,
  string UserName,
  long TelegramId);

public record GetNextTaskExecutorQuery(Guid FamilyId, Guid TaskId)
  : IQuery<Result<NextTaskExecutorResult?>>;

public class GetNextTaskExecutorHandler(
  IAppRepository<TaskInstance> taskAppRepository,
  IAppRepository<Family> familyAppRepository)
  : IQueryHandler<GetNextTaskExecutorQuery, Result<NextTaskExecutorResult?>>
{
  public async ValueTask<Result<NextTaskExecutorResult?>> Handle(GetNextTaskExecutorQuery query,
    CancellationToken cancellationToken)
  {
    var task = await taskAppRepository.GetByIdAsync(query.TaskId, cancellationToken);
    if (task is null || !task.TemplateId.HasValue)
      return Result.Success<NextTaskExecutorResult?>(null);

    if (task.FamilyId != query.FamilyId)
      return Result.Success<NextTaskExecutorResult?>(null);

    var family = await familyAppRepository.GetByIdAsync(query.FamilyId, cancellationToken);
    if (family is null)
      return Result.Success<NextTaskExecutorResult?>(null);

    var activeMembers = family.Members
      .Where(m => m.IsActive)
      .ToList();

    if (activeMembers.Count == 0)
      return Result.Success<NextTaskExecutorResult?>(null);

    var completedSpec = new CompletedTaskInstancesByTemplateAndFamilySpec(query.FamilyId, task.TemplateId.Value);
    var completedInstances = await taskAppRepository.ListAsync(completedSpec, cancellationToken);

    var candidate = SelectMemberForReminder(activeMembers, completedInstances);
    if (candidate?.User is null)
      return Result.Success<NextTaskExecutorResult?>(null);

    var user = candidate.User;

    var result = new NextTaskExecutorResult(
      candidate.Id,
      candidate.UserId,
      user.Name,
      user.TelegramId);

    return Result.Success<NextTaskExecutorResult?>(result);
  }

  private static FamilyMember? SelectMemberForReminder(
    IReadOnlyCollection<FamilyMember> activeMembers,
    IReadOnlyCollection<TaskInstance> completedInstances)
  {
    if (activeMembers.Count == 0) return null;

    var lastCompletedByMember = completedInstances
      .Where(t => t.CompletedByMemberId.HasValue && t.CompletedAt.HasValue)
      .GroupBy(t => t.CompletedByMemberId!.Value)
      .ToDictionary(
        g => g.Key,
        g => g.Max(t => t.CompletedAt!.Value));

    FamilyMember? candidate = null;
    var candidateLastCompleted = DateTime.MaxValue;

    foreach (var member in activeMembers)
    {
      var hasLast = lastCompletedByMember.TryGetValue(member.Id, out var lastCompleted);
      var effectiveLastCompleted = hasLast ? lastCompleted : DateTime.MinValue;

      if (candidate is null || effectiveLastCompleted < candidateLastCompleted)
      {
        candidate = member;
        candidateLastCompleted = effectiveLastCompleted;
      }
    }

    return candidate;
  }
}
