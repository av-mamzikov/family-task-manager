namespace FamilyTaskManager.UseCases.Families;

public record GetFamilyMembersQuery(Guid FamilyId) : IQuery<Result<List<FamilyMemberDto>>>;

public class GetFamilyMembersHandler(
  IAppRepository<Family> familyAppRepository,
  IAppRepository<User> userAppRepository)
  : IQueryHandler<GetFamilyMembersQuery, Result<List<FamilyMemberDto>>>
{
  public async ValueTask<Result<List<FamilyMemberDto>>> Handle(GetFamilyMembersQuery query,
    CancellationToken cancellationToken)
  {
    var spec = new GetFamilyWithMembersSpec(query.FamilyId);
    var family = await familyAppRepository.FirstOrDefaultAsync(spec, cancellationToken);

    if (family == null) return Result<List<FamilyMemberDto>>.NotFound("Семья не найдена");

    var members = new List<FamilyMemberDto>();

    foreach (var member in family.Members.Where(m => m.IsActive))
    {
      var user = await userAppRepository.GetByIdAsync(member.UserId, cancellationToken);
      var name = user?.Name ?? "Неизвестный пользователь";

      members.Add(new(member.Id, member.UserId, member.FamilyId, name, member.Role, member.Points));
    }

    return Result<List<FamilyMemberDto>>.Success(members);
  }
}
