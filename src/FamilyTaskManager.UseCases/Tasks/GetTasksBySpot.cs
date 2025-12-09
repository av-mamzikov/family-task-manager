using FamilyTaskManager.Core.TaskAggregate.DTOs;
using FamilyTaskManager.Core.TaskAggregate.Specifications;

namespace FamilyTaskManager.UseCases.Tasks;

public record GetTasksBySpotQuery(Guid SpotId, Guid FamilyId, TaskStatus? Status = null)
  : IQuery<Result<List<TaskDto>>>;

public class GetTasksBySpotHandler(
  IAppReadRepository<TaskInstance> taskRepository,
  IAppRepository<Spot> spotAppRepository)
  : IQueryHandler<GetTasksBySpotQuery, Result<List<TaskDto>>>
{
  public async ValueTask<Result<List<TaskDto>>> Handle(GetTasksBySpotQuery request,
    CancellationToken cancellationToken)
  {
    // Verify spot exists and belongs to family
    var spot = await spotAppRepository.GetByIdAsync(request.SpotId, cancellationToken);
    if (spot == null || spot.FamilyId != request.FamilyId) return Result<List<TaskDto>>.NotFound("Spot not found");

    var spec = new TasksDtoBySpotSpec(request.SpotId, request.Status);
    var tasks = await taskRepository.ListAsync(spec, cancellationToken);

    return Result<List<TaskDto>>.Success(tasks);
  }
}
