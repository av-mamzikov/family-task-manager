using FamilyTaskManager.Core.TaskAggregate.DTOs;

namespace FamilyTaskManager.UseCases.Tasks;

public record GetActiveTasksQuery(Guid FamilyId, Guid UserId) : IQuery<Result<List<TaskDto>>>;

public class GetActiveTasksHandler(
  IRepository<TaskInstance> taskRepository,
  IRepository<Pet> petRepository,
  IRepository<Family> familyRepository) : IQueryHandler<GetActiveTasksQuery, Result<List<TaskDto>>>
{
  public async ValueTask<Result<List<TaskDto>>> Handle(GetActiveTasksQuery query, CancellationToken cancellationToken)
  {
    var spec = new GetActiveTasksByFamilyWithMembersSpec(query.FamilyId);
    var tasks = await taskRepository.ListAsync(spec, cancellationToken);

    // Get all pets for this family
    var petSpec = new GetPetsByFamilySpec(query.FamilyId);
    var pets = await petRepository.ListAsync(petSpec, cancellationToken);
    var petDict = pets.ToDictionary(p => p.Id, p => p.Name);

    // Get current user's member ID and family timezone
    var familySpec = new GetFamilyWithMembersSpec(query.FamilyId);
    var family = await familyRepository.FirstOrDefaultAsync(familySpec, cancellationToken);
    var currentMember = family?.Members.FirstOrDefault(m => m.UserId == query.UserId && m.IsActive);
    var currentMemberId = currentMember?.Id;
    var familyTimezone = family?.Timezone ?? "UTC";

    var result = tasks.Select(t => new TaskDto(
      t.Id,
      t.Title,
      t.Points,
      t.Status,
      t.DueAt,
      t.PetId,
      petDict.GetValueOrDefault(t.PetId, "Unknown"),
      t.StartedByMemberId,
      familyTimezone,
      t.StartedByMember?.User?.Name,
      t.StartedByMemberId.HasValue && t.StartedByMemberId.Value == currentMemberId
    )).ToList();

    return Result<List<TaskDto>>.Success(result);
  }
}
