namespace FamilyTaskManager.UseCases.Tasks;

public record GetTaskByIdQuery(Guid Id, Guid FamilyId)
  : IQuery<Result<TaskDto>>;

public class GetTaskByIdHandler(
  IRepository<TaskInstance> taskRepository,
  IRepository<Pet> petRepository)
  : IQueryHandler<GetTaskByIdQuery, Result<TaskDto>>
{
  public async ValueTask<Result<TaskDto>> Handle(GetTaskByIdQuery request,
    CancellationToken cancellationToken)
  {
    var spec = new GetTaskByIdWithMembersSpec(request.Id);
    var task = await taskRepository.FirstOrDefaultAsync(spec, cancellationToken);

    if (task == null)
    {
      return Result<TaskDto>.NotFound("Task not found");
    }

    // Authorization check - ensure task belongs to the requested family
    if (task.FamilyId != request.FamilyId)
    {
      return Result<TaskDto>.NotFound("Task not found");
    }

    // Get pet name
    var pet = await petRepository.GetByIdAsync(task.PetId, cancellationToken);
    var petName = pet?.Name ?? "Unknown";

    var result = new TaskDto(
      task.Id,
      task.Title,
      task.Points,
      task.Status,
      task.DueAt,
      task.PetId,
      petName,
      task.StartedByMemberId,
      task.StartedByMember?.User?.Name,
      false); // Cannot determine if current user can complete without userId

    return Result<TaskDto>.Success(result);
  }
}
