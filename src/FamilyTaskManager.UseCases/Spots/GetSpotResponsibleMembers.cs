using FamilyTaskManager.UseCases.Contracts;

namespace FamilyTaskManager.UseCases.Spots;

public record GetSpotResponsibleMembersQuery(Guid SpotId)
  : IQuery<Result<List<FamilyMemberDto>>>;

public class GetSpotResponsibleMembersHandler(
  IReadOnlyEntityRepository<FamilyMember> familyMemberEntityRepository)
  : IQueryHandler<GetSpotResponsibleMembersQuery, Result<List<FamilyMemberDto>>>
{
  public async ValueTask<Result<List<FamilyMemberDto>>> Handle(
    GetSpotResponsibleMembersQuery query,
    CancellationToken cancellationToken)
  {
    var spec = new GetSpotResponsibleMembersDtoSpec(query.SpotId);
    var members = await familyMemberEntityRepository.ListProjectionAsync(spec, cancellationToken);

    return Result<List<FamilyMemberDto>>.Success(members);
  }
}
