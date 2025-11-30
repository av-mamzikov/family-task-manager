using FamilyTaskManager.Core.Interfaces;

namespace FamilyTaskManager.UseCases.Families;

public record GetFamilyMemberByIdQuery(Guid FamilyMemberId)
  : IQuery<Result<FamilyMemberDto>>;

public class GetFamilyMemberByIdHandler(
  IReadOnlyEntityRepository<FamilyMember> familyMemberEntityRepository)
  : IQueryHandler<GetFamilyMemberByIdQuery, Result<FamilyMemberDto>>
{
  public async ValueTask<Result<FamilyMemberDto>> Handle(
    GetFamilyMemberByIdQuery query,
    CancellationToken cancellationToken)
  {
    var spec = new GetFamilyMemberDtoByIdSpec(query.FamilyMemberId);
    var memberDto = await familyMemberEntityRepository.FirstOrDefaultProjectionAsync(spec, cancellationToken);

    if (memberDto == null)
    {
      return Result<FamilyMemberDto>.NotFound("Участник семьи не найден");
    }

    return Result<FamilyMemberDto>.Success(memberDto);
  }
}
