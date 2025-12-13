using FamilyTaskManager.UseCases.Features.FamilyManagement.Dtos;

namespace FamilyTaskManager.UseCases.Features.SpotManagement.Queries;

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

public class GetSpotResponsibleMembersDtoSpec : Specification<FamilyMember, FamilyMemberDto>
{
  public GetSpotResponsibleMembersDtoSpec(Guid spotId)
  {
    Query
      .Where(m => m.IsActive && m.ResponsibleSpots.Any(s => s.Id == spotId))
      .Select(FamilyMemberDto.Projections.FromFamilyMember);
  }
}
