using FamilyTaskManager.UseCases.Features.FamilyManagement.Dtos;

namespace FamilyTaskManager.UseCases.Features.TaskTemplatesManagement.Queries;

public record GetTaskTemplateResponsibleMembersQuery(Guid TaskTemplateId)
  : IQuery<Result<List<FamilyMemberDto>>>;

public class GetTaskTemplateResponsibleMembersHandler(
  IReadOnlyEntityRepository<FamilyMember> familyMemberEntityRepository)
  : IQueryHandler<GetTaskTemplateResponsibleMembersQuery, Result<List<FamilyMemberDto>>>
{
  public async ValueTask<Result<List<FamilyMemberDto>>> Handle(
    GetTaskTemplateResponsibleMembersQuery query,
    CancellationToken cancellationToken)
  {
    var spec = new GetTaskTemplateResponsibleMembersDtoSpec(query.TaskTemplateId);
    var members = await familyMemberEntityRepository.ListProjectionAsync(spec, cancellationToken);

    return Result<List<FamilyMemberDto>>.Success(members);
  }
}

public class GetTaskTemplateResponsibleMembersDtoSpec : Specification<FamilyMember, FamilyMemberDto>
{
  public GetTaskTemplateResponsibleMembersDtoSpec(Guid taskTemplateId)
  {
    Query
      .Where(m => m.IsActive && m.ResponsibleTaskTemplates.Any(t => t.Id == taskTemplateId))
      .Select(FamilyMemberDto.Projections.FromFamilyMember);
  }
}