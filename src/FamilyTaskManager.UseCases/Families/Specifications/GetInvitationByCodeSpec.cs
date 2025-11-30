namespace FamilyTaskManager.UseCases.Families.Specifications;

public class GetInvitationByCodeSpec : Specification<Invitation>
{
  public GetInvitationByCodeSpec(string code)
  {
    Query.Where(i => i.Code == code);
  }
}
