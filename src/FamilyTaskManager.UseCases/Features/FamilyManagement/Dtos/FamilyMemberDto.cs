using System.Linq.Expressions;

namespace FamilyTaskManager.UseCases.Features.FamilyManagement.Dtos;

public record FamilyMemberDto(
  Guid Id,
  Guid UserId,
  Guid FamilyId,
  string UserName,
  FamilyRole Role,
  int Points)
{
  public static class Projections
  {
    public static readonly Expression<Func<FamilyMember, FamilyMemberDto>> FromFamilyMember =
      m => new(m.Id, m.UserId, m.FamilyId, m.User.Name, m.Role, m.Points);
  }
}
