using FamilyTaskManager.Core.TaskAggregate.DTOs;

namespace FamilyTaskManager.UseCases.Tasks;

public record GetTaskByIdQuery(Guid Id, Guid FamilyId)
  : IQuery<Result<TaskDto>>;

public class GetTaskByIdHandler(IRepository<TaskInstance> taskRepository)
  : IQueryHandler<GetTaskByIdQuery, Result<TaskDto>>
{
  public async ValueTask<Result<TaskDto>> Handle(GetTaskByIdQuery request,
    CancellationToken cancellationToken)
  {
    var spec = new GetTaskByIdWithMembersSpec(request.Id);
    var task = await taskRepository.FirstOrDefaultAsync(spec, cancellationToken);

    if (task == null) return Result<TaskDto>.NotFound("Task not found");

    // Authorization check - ensure task belongs to the requested family
    // Note: We need to load the entity to check FamilyId since it's not in DTO
    var taskEntity = await taskRepository.GetByIdAsync(request.Id, cancellationToken);
    if (taskEntity == null || taskEntity.FamilyId != request.FamilyId)
      return Result<TaskDto>.NotFound("Task not found");

    return Result<TaskDto>.Success(task);
  }
}
