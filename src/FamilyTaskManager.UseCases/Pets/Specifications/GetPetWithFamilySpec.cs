namespace FamilyTaskManager.UseCases.Pets.Specifications;

public class GetPetWithFamilySpec : Specification<Pet>
{
  public GetPetWithFamilySpec(Guid petId)
  {
    Query
      .Where(p => p.Id == petId)
      .Include(p => p.Family);
  }
}
