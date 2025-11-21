namespace FamilyTaskManager.UseCases.Pets.Specifications;

public class GetPetsByFamilySpec : Specification<Pet>
{
  public GetPetsByFamilySpec(Guid familyId)
  {
    Query.Where(p => p.FamilyId == familyId);
  }
}
