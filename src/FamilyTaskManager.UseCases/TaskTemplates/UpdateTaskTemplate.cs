namespace FamilyTaskManager.UseCases.TaskTemplates;

public record UpdateTaskTemplateCommand(
  Guid TemplateId,
  Guid FamilyId,
  string? Title,
  int? Points,
  string? Schedule,
  TimeSpan? DueDuration) : ICommand<Result>;

public class UpdateTaskTemplateHandler(IRepository<TaskTemplate> templateRepository)
  : ICommandHandler<UpdateTaskTemplateCommand, Result>
{
  public async ValueTask<Result> Handle(UpdateTaskTemplateCommand command, CancellationToken cancellationToken)
  {
    var template = await templateRepository.GetByIdAsync(command.TemplateId, cancellationToken);
    if (template == null)
    {
      return Result.NotFound("Шаблон задачи не найден");
    }

    // Authorization check - ensure template belongs to the requested family
    if (template.FamilyId != command.FamilyId)
    {
      return Result.NotFound("Шаблон задачи не найден");
    }

    if (!template.IsActive)
    {
      return Result.Error("Нельзя изменить неактивный шаблон");
    }

    // Get current values or use new ones
    var newTitle = command.Title ?? template.Title;
    var newPoints = command.Points ?? template.Points;
    var newSchedule = command.Schedule ?? template.Schedule;

    // Validate title
    if (newTitle.Length < 3 || newTitle.Length > 100)
    {
      return Result.Invalid(new ValidationError("Название должно быть длиной от 3 до 100 символов"));
    }

    // Validate points
    if (newPoints < 1 || newPoints > 100)
    {
      return Result.Invalid(new ValidationError("Очки должны быть в диапазоне от 1 до 100"));
    }

    // Validate schedule
    if (string.IsNullOrWhiteSpace(newSchedule))
    {
      return Result.Invalid(new ValidationError("Расписание не может быть пустым"));
    }

    // Update template
    var newDueDuration = command.DueDuration ?? template.DueDuration;

    // Validate dueDuration
    if (newDueDuration < TimeSpan.Zero || newDueDuration > TimeSpan.FromHours(24))
      return Result.Invalid(new ValidationError("Срок выполнения должен быть в диапазоне от 0 до 24 часов"));

    template.Update(newTitle, newPoints, newSchedule, newDueDuration);
    await templateRepository.UpdateAsync(template, cancellationToken);

    return Result.Success();
  }
}
