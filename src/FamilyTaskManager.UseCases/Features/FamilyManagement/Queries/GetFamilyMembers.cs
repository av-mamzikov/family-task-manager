using FamilyTaskManager.UseCases.Features.FamilyManagement.Dtos;

namespace FamilyTaskManager.UseCases.Features.FamilyManagement.Queries;

public record GetFamilyMembersQuery(Guid FamilyId) : IQuery<Result<List<FamilyMemberDto>>>;

public class GetFamilyMembersHandler(
  IAppRepository<Family> familyAppRepository)
  : IQueryHandler<GetFamilyMembersQuery, Result<List<FamilyMemberDto>>>
{
  public async ValueTask<Result<List<FamilyMemberDto>>> Handle(GetFamilyMembersQuery query,
    CancellationToken cancellationToken)
  {
    var family = await familyAppRepository.GetByIdAsync(query.FamilyId, cancellationToken);

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
