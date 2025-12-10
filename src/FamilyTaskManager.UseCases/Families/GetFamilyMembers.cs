using FamilyTaskManager.UseCases.Contracts;

namespace FamilyTaskManager.UseCases.Families;

public record GetFamilyMembersQuery(Guid FamilyId) : IQuery<Result<List<FamilyMemberDto>>>;

public class GetFamilyMembersHandler(
  IAppRepository<Family> familyAppRepository)
  : IQueryHandler<GetFamilyMembersQuery, Result<List<FamilyMemberDto>>>
{
  public async ValueTask<Result<List<FamilyMemberDto>>> Handle(GetFamilyMembersQuery query,
    CancellationToken cancellationToken)
  {
    var spec = new GetFamilyWithMembersSpec(query.FamilyId);
    var family = await familyAppRepository.FirstOrDefaultAsync(spec, cancellationToken);

    if (family == null) return Result<List<FamilyMemberDto>>.NotFound("Семья не найдена");

    var members = family.Members
      .Where(m => m.IsActive)
      .OrderBy(m => m.Role).ThenBy(m => m.User.Name)
      .Select(member =>
        new FamilyMemberDto(member.Id, member.UserId, member.FamilyId, member.User.Name, member.Role, member.Points))
      .ToList();

    return Result<List<FamilyMemberDto>>.Success(members);
  }
}
