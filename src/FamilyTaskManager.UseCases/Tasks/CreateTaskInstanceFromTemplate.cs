namespace FamilyTaskManager.UseCases.Tasks;

public record CreateTaskInstanceFromTemplateCommand(Guid TemplateId, DateTime DueAt) : ICommand<Result<Guid>>;

public class CreateTaskInstanceFromTemplateHandler(IRepository<TaskTemplate> templateRepository, IRepository<TaskInstance> taskRepository)
  : ICommandHandler<CreateTaskInstanceFromTemplateCommand, Result<Guid>>
{
  public async ValueTask<Result<Guid>> Handle(CreateTaskInstanceFromTemplateCommand request, CancellationToken cancellationToken)
  {
    var template = await templateRepository.GetByIdAsync(request.TemplateId, cancellationToken);
    if (template == null)
    {
      return Result.NotFound($"TaskTemplate with ID {request.TemplateId} not found.");
    }

    if (!template.IsActive)
    {
      return Result.Error("TaskTemplate is not active.");
    }

    // Check if there's already an active TaskInstance for this template
    var spec = new TaskInstancesByTemplateSpec(request.TemplateId);
    var existingInstances = await taskRepository.ListAsync(spec, cancellationToken);
    
    var activeInstance = existingInstances.FirstOrDefault(t => t.Status != TaskStatus.Completed);
    if (activeInstance != null)
    {
      // Don't create a new instance if there's already an active one
      return Result.Error($"Active TaskInstance already exists for template {request.TemplateId}");
    }

    var taskInstance = new TaskInstance(
      template.FamilyId,
      template.PetId,
      template.Title,
      template.Points,
      TaskType.Recurring,
      request.DueAt,
      request.TemplateId
    );

    await taskRepository.AddAsync(taskInstance, cancellationToken);
    await taskRepository.SaveChangesAsync(cancellationToken);

    return Result.Success(taskInstance.Id);
  }
}
