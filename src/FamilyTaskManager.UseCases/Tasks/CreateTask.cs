namespace FamilyTaskManager.UseCases.Tasks;

public record CreateTaskCommand(
  Guid FamilyId, 
  Guid PetId, 
  string Title, 
  int Points, 
  DateTime DueAt,
  Guid CreatedBy) : ICommand<Result<Guid>>;

public class CreateTaskHandler(
  IRepository<TaskInstance> taskRepository,
  IRepository<Pet> petRepository) : ICommandHandler<CreateTaskCommand, Result<Guid>>
{
  public async ValueTask<Result<Guid>> Handle(CreateTaskCommand command, CancellationToken cancellationToken)
  {
    // Verify pet exists and belongs to family
    var pet = await petRepository.GetByIdAsync(command.PetId, cancellationToken);
    if (pet == null)
    {
      return Result<Guid>.NotFound("Pet not found");
    }

    if (pet.FamilyId != command.FamilyId)
    {
      return Result<Guid>.Error("Pet does not belong to this family");
    }

    // Validate title length
    if (command.Title.Length < 3 || command.Title.Length > 100)
    {
      return Result<Guid>.Invalid(new ValidationError("Title must be between 3 and 100 characters"));
    }

    // Validate points range
    if (command.Points < 1 || command.Points > 100)
    {
      return Result<Guid>.Invalid(new ValidationError("Points must be between 1 and 100"));
    }

    // Create one-time task
    var task = new TaskInstance(
      command.FamilyId,
      command.PetId,
      command.Title,
      command.Points,
      TaskType.OneTime,
      command.DueAt);

    await taskRepository.AddAsync(task, cancellationToken);

    return Result<Guid>.Success(task.Id);
  }
}
