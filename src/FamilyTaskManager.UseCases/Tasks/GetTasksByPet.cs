using FamilyTaskManager.Core.TaskAggregate.DTOs;
using FamilyTaskManager.Core.TaskAggregate.Specifications;

namespace FamilyTaskManager.UseCases.Tasks;

public record GetTasksByPetQuery(Guid PetId, Guid FamilyId, TaskStatus? Status = null)
  : IQuery<Result<List<TaskDto>>>;

public class GetTasksByPetHandler(IReadRepository<TaskInstance> taskRepository, IAppRepository<Pet> petAppRepository)
  : IQueryHandler<GetTasksByPetQuery, Result<List<TaskDto>>>
{
  public async ValueTask<Result<List<TaskDto>>> Handle(GetTasksByPetQuery request,
    CancellationToken cancellationToken)
  {
    // Verify pet exists and belongs to family
    var pet = await petAppRepository.GetByIdAsync(request.PetId, cancellationToken);
    if (pet == null || pet.FamilyId != request.FamilyId) return Result<List<TaskDto>>.NotFound("Pet not found");

    var spec = new TasksDtoByPetSpec(request.PetId, request.Status);
    var tasks = await taskRepository.ListAsync(spec, cancellationToken);

    return Result<List<TaskDto>>.Success(tasks);
  }
}
