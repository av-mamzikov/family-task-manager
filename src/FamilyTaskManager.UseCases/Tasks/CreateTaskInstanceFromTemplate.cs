using FamilyTaskManager.Core.Services;

namespace FamilyTaskManager.UseCases.Tasks;

public record CreateTaskInstanceFromTemplateCommand(Guid TemplateId, DateTime DueAt) : ICommand<Result<Guid>>;

public class CreateTaskInstanceFromTemplateHandler(
  IAppRepository<TaskTemplate> templateAppRepository,
  IAppRepository<TaskInstance> taskAppRepository,
  ITaskInstanceFactory taskInstanceFactory,
  IAppRepository<SpotBowsing> SpotAppRepository,
  ISpotMoodCalculator moodCalculator)
  : ICommandHandler<CreateTaskInstanceFromTemplateCommand, Result<Guid>>
{
  public async ValueTask<Result<Guid>> Handle(CreateTaskInstanceFromTemplateCommand request,
    CancellationToken cancellationToken)
  {
    var template = await templateAppRepository.GetByIdAsync(request.TemplateId, cancellationToken);
    if (template == null)
      return Result.NotFound($"TaskTemplate with ID {request.TemplateId} not found.");

    // Load Spot with family (needed for TaskCreatedEvent)
    var SpotSpec = new GetSpotByIdWithFamilySpec(template.SpotId);
    var Spot = await SpotAppRepository.FirstOrDefaultAsync(SpotSpec, cancellationToken);
    if (Spot == null)
      return Result.NotFound($"Spot with ID {template.SpotId} not found.");

    var spec = new TaskInstancesByTemplateSpec(request.TemplateId);
    var existingInstances = await taskAppRepository.ListAsync(spec, cancellationToken);
    var createResult = taskInstanceFactory.CreateFromTemplate(template, Spot, request.DueAt, existingInstances);
    if (!createResult.IsSuccess)
      return Result.Error(string.Join(", ", createResult.Errors));

    await taskAppRepository.AddAsync(createResult.Value, cancellationToken);
    await taskAppRepository.SaveChangesAsync(cancellationToken);

    // Trigger immediate mood recalculation for the Spot
    var newMoodScore = await moodCalculator.CalculateMoodScoreAsync(Spot.Id, cancellationToken);
    Spot.UpdateMoodScore(newMoodScore);
    await SpotAppRepository.SaveChangesAsync(cancellationToken);

    return Result.Success(createResult.Value.Id);
  }
}
