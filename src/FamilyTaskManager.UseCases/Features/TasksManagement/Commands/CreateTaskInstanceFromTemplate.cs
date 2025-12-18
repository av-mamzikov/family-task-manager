using FamilyTaskManager.Core.Services;
using FamilyTaskManager.Core.SpotAggregate.Specifications;
using FamilyTaskManager.Core.TaskAggregate.Specifications;
using FamilyTaskManager.UseCases.Features.TasksManagement.Services;

namespace FamilyTaskManager.UseCases.Features.TasksManagement.Commands;

public record CreateTaskInstanceFromTemplateCommand(Guid TemplateId, DateTime DueAt) : ICommand<Result<TaskInstance>>;

public class CreateTaskInstanceFromTemplateHandler(
  IAppRepository<TaskTemplate> templateAppRepository,
  IAppRepository<TaskInstance> taskAppRepository,
  ITaskInstanceFactory taskInstanceFactory,
  IAppRepository<Spot> spotAppRepository,
  ISpotMoodCalculator moodCalculator,
  IAssignedMemberSelector assignedMemberSelector)
  : ICommandHandler<CreateTaskInstanceFromTemplateCommand, Result<TaskInstance>>
{
  public async ValueTask<Result<TaskInstance>> Handle(CreateTaskInstanceFromTemplateCommand request,
    CancellationToken cancellationToken)
  {
    var template = await templateAppRepository.GetByIdAsync(request.TemplateId, cancellationToken);
    if (template == null)
      return Result.NotFound($"TaskTemplate with ID {request.TemplateId} not found.");

    // Load Spot with family (needed for TaskCreatedEvent)
    var spotSpec = new GetSpotByIdWithFamilySpec(template.SpotId);
    var spot = await spotAppRepository.FirstOrDefaultAsync(spotSpec, cancellationToken);
    if (spot == null)
      return Result.NotFound($"Spot with ID {template.SpotId} not found.");

    var spec = new TaskInstancesByTemplateSpec(request.TemplateId);
    var existingInstances = await taskAppRepository.ListAsync(spec, cancellationToken);

    var assignedToMemberId =
      await assignedMemberSelector.SelectAssignedMemberAsync(template, spot, cancellationToken);

    var createResult = taskInstanceFactory.CreateFromTemplate(template, spot, request.DueAt, existingInstances,
      assignedToMemberId);
    if (!createResult.IsSuccess)
      return Result.Error(string.Join(", ", createResult.Errors));

    await taskAppRepository.AddAsync(createResult.Value, cancellationToken);
    await taskAppRepository.SaveChangesAsync(cancellationToken);

    // Trigger immediate mood recalculation for the Spot
    var newMoodScore = await moodCalculator.CalculateMoodScoreAsync(spot.Id, cancellationToken);
    spot.UpdateMoodScore(newMoodScore);
    await spotAppRepository.SaveChangesAsync(cancellationToken);

    return Result.Success(createResult.Value);
  }
}
