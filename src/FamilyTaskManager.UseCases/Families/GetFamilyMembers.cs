namespace FamilyTaskManager.UseCases.Families;

public record FamilyMemberDto(
  Guid UserId,
  string Name,
  FamilyRole Role,
  int Points);

public record GetFamilyMembersQuery(Guid FamilyId) : IQuery<Result<List<FamilyMemberDto>>>;

public class GetFamilyMembersHandler(
  IRepository<Family> familyRepository,
  IRepository<User> userRepository)
  : IQueryHandler<GetFamilyMembersQuery, Result<List<FamilyMemberDto>>>
{
  public async ValueTask<Result<List<FamilyMemberDto>>> Handle(GetFamilyMembersQuery query,
    CancellationToken cancellationToken)
  {
    var spec = new GetFamilyWithMembersSpec(query.FamilyId);
    var family = await familyRepository.FirstOrDefaultAsync(spec, cancellationToken);

    if (family == null)
    {
      return Result<List<FamilyMemberDto>>.NotFound("Семья не найдена");
    }

    var members = new List<FamilyMemberDto>();

    foreach (var member in family.Members.Where(m => m.IsActive))
    {
      var user = await userRepository.GetByIdAsync(member.UserId, cancellationToken);
      var name = user?.Name ?? "Неизвестный пользователь";

      members.Add(new FamilyMemberDto(member.UserId, name, member.Role, member.Points));
    }

    return Result<List<FamilyMemberDto>>.Success(members);
  }
}
