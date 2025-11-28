using FamilyTaskManager.Core.TaskAggregate.Specifications;

namespace FamilyTaskManager.UseCases.Tasks;

public record GetTasksByPetQuery(Guid PetId, Guid FamilyId, TaskStatus? Status = null)
  : IQuery<Result<List<TaskDto>>>;

public class GetTasksByPetHandler(IRepository<TaskInstance> taskRepository, IRepository<Pet> petRepository)
  : IQueryHandler<GetTasksByPetQuery, Result<List<TaskDto>>>
{
  public async ValueTask<Result<List<TaskDto>>> Handle(GetTasksByPetQuery request,
    CancellationToken cancellationToken)
  {
    // Verify pet exists and belongs to family
    var pet = await petRepository.GetByIdAsync(request.PetId, cancellationToken);
    if (pet == null)
    {
      return Result<List<TaskDto>>.NotFound("Pet not found");
    }

    if (pet.FamilyId != request.FamilyId)
    {
      return Result<List<TaskDto>>.NotFound("Pet not found");
    }

    var spec = new TasksByPetSpec(request.PetId, request.Status);
    var tasks = await taskRepository.ListAsync(spec, cancellationToken);

    if (tasks.Count == 0)
    {
      return Result<List<TaskDto>>.Success(new List<TaskDto>());
    }

    var result = tasks.Select(t => new TaskDto(
      t.Id,
      t.Title,
      t.Points,
      t.Status,
      t.DueAt,
      t.PetId,
      pet.Name)).ToList();

    return Result<List<TaskDto>>.Success(result);
  }
}
