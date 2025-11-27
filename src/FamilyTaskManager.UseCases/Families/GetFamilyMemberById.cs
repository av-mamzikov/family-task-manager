namespace FamilyTaskManager.UseCases.Families;

public record GetFamilyMemberByIdQuery(Guid FamilyMemberId)
  : IQuery<Result<FamilyMemberDto>>;

public class GetFamilyMemberByIdHandler(
  IRepository<User> userRepository)
  : IQueryHandler<GetFamilyMemberByIdQuery, Result<FamilyMemberDto>>
{
  public async ValueTask<Result<FamilyMemberDto>> Handle(
    GetFamilyMemberByIdQuery query,
    CancellationToken cancellationToken)
  {
    var spec = new GetUserByMemberIdWithMemberSpec(query.FamilyMemberId);
    var user = await userRepository.FirstOrDefaultAsync(spec, cancellationToken);
    if (user == null)
    {
      return Result<FamilyMemberDto>.NotFound("Пользователь не найден");
    }

    var member = user.FamilyMembers.FirstOrDefault(m => m.Id == query.FamilyMemberId);
    if (member == null)
    {
      return Result<FamilyMemberDto>.NotFound("Участник семьи не найден");
    }

    var dto = new FamilyMemberDto(member.Id, user.Id, member.FamilyId, user.Name, member.Role, member.Points);
    return Result<FamilyMemberDto>.Success(dto);
  }
}
