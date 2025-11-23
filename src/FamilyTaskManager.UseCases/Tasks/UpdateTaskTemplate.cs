namespace FamilyTaskManager.UseCases.Tasks;

public record UpdateTaskTemplateCommand(
  Guid TemplateId,
  string? Title,
  int? Points,
  string? Schedule) : ICommand<Result>;

public class UpdateTaskTemplateHandler(IRepository<TaskTemplate> templateRepository)
  : ICommandHandler<UpdateTaskTemplateCommand, Result>
{
  public async ValueTask<Result> Handle(UpdateTaskTemplateCommand command, CancellationToken cancellationToken)
  {
    var template = await templateRepository.GetByIdAsync(command.TemplateId, cancellationToken);
    if (template == null)
    {
      return Result.NotFound("Task template not found");
    }

    if (!template.IsActive)
    {
      return Result.Error("Cannot update inactive template");
    }

    // Get current values or use new ones
    var newTitle = command.Title ?? template.Title;
    var newPoints = command.Points ?? template.Points;
    var newSchedule = command.Schedule ?? template.Schedule;

    // Validate title
    if (newTitle.Length < 3 || newTitle.Length > 100)
    {
      return Result.Invalid(new ValidationError("Title must be between 3 and 100 characters"));
    }

    // Validate points
    if (newPoints < 1 || newPoints > 100)
    {
      return Result.Invalid(new ValidationError("Points must be between 1 and 100"));
    }

    // Validate schedule
    if (string.IsNullOrWhiteSpace(newSchedule))
    {
      return Result.Invalid(new ValidationError("Schedule cannot be empty"));
    }

    // Update template
    template.Update(newTitle, newPoints, newSchedule);
    await templateRepository.UpdateAsync(template, cancellationToken);

    return Result.Success();
  }
}
