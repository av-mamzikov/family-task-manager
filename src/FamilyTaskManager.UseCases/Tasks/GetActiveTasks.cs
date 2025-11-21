namespace FamilyTaskManager.UseCases.Tasks;

public record TaskDto(
  Guid Id, 
  string Title, 
  int Points, 
  TaskStatus Status, 
  DateTime DueAt,
  Guid PetId,
  string PetName);

public record GetActiveTasksQuery(Guid FamilyId) : IQuery<Result<List<TaskDto>>>;

public class GetActiveTasksHandler(
  IRepository<TaskInstance> taskRepository,
  IRepository<Pet> petRepository) : IQueryHandler<GetActiveTasksQuery, Result<List<TaskDto>>>
{
  public async ValueTask<Result<List<TaskDto>>> Handle(GetActiveTasksQuery query, CancellationToken cancellationToken)
  {
    var spec = new GetActiveTasksByFamilySpec(query.FamilyId);
    var tasks = await taskRepository.ListAsync(spec, cancellationToken);

    // Get all pets for this family
    var petSpec = new GetPetsByFamilySpec(query.FamilyId);
    var pets = await petRepository.ListAsync(petSpec, cancellationToken);
    var petDict = pets.ToDictionary(p => p.Id, p => p.Name);

    var result = tasks.Select(t => new TaskDto(
      t.Id,
      t.Title,
      t.Points,
      t.Status,
      t.DueAt,
      t.PetId,
      petDict.GetValueOrDefault(t.PetId, "Unknown")
    )).ToList();

    return Result<List<TaskDto>>.Success(result);
  }
}
