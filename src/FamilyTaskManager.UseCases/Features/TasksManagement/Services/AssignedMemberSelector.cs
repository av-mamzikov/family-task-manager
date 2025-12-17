namespace FamilyTaskManager.UseCases.Features.TasksManagement.Services;

public class AssignedMemberSelector(ITaskCompletionStatsQuery statsQuery) : IAssignedMemberSelector
{
  public async ValueTask<FamilyMember?> SelectAssignedMemberAsync(TaskTemplate template, Spot spot,
    CancellationToken cancellationToken)
  {
    var templateCandidates = template.ResponsibleMembers
      .Where(m => m.IsActive)
      .ToList();

    if (templateCandidates.Count > 0)
    {
      var lastCompletedByMember = await statsQuery.GetLastCompletedAtByMemberForTemplateAsync(
        template.FamilyId,
        template.Id,
        templateCandidates.Select(m => m.Id).ToList(),
        cancellationToken);

      var candidateId = SelectCandidateIdByOldestLastCompletion(templateCandidates, lastCompletedByMember);
      return candidateId;
    }

    var spotCandidates = spot.ResponsibleMembers
      .Where(m => m.IsActive)
      .ToList();

    if (spotCandidates.Count == 0)
      return null;

    var spotLastCompletedByMember = await statsQuery.GetLastCompletedAtByMemberForSpotAsync(
      spot.FamilyId,
      spot.Id,
      spotCandidates.Select(m => m.Id).ToList(),
      cancellationToken);

    return SelectCandidateIdByOldestLastCompletion(spotCandidates, spotLastCompletedByMember);
  }

  private static FamilyMember? SelectCandidateIdByOldestLastCompletion(
    IReadOnlyCollection<FamilyMember> candidates,
    IReadOnlyDictionary<Guid, DateTime> lastCompletedByMember)
  {
    if (candidates.Count == 0) return null;

    FamilyMember? candidate = null;
    var candidateLastCompleted = DateTime.MaxValue;

    foreach (var member in candidates)
    {
      var hasLast = lastCompletedByMember.TryGetValue(member.Id, out var lastCompleted);
      var effectiveLastCompleted = hasLast ? lastCompleted : DateTime.MinValue;

      if (candidate == null || effectiveLastCompleted < candidateLastCompleted)
      {
        candidate = member;
        candidateLastCompleted = effectiveLastCompleted;
      }
    }

    return candidate;
  }
}
