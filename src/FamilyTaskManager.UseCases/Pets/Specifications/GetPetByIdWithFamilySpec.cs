namespace FamilyTaskManager.UseCases.Pets.Specifications;

public class GetPetByIdWithFamilySpec : Specification<Pet>
{
  public GetPetByIdWithFamilySpec(Guid petId)
  {
    Query
      .Where(p => p.Id == petId)
      .Include(p => p.Family);
  }
}
