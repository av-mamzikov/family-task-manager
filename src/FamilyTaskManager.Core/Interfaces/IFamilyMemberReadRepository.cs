using FamilyTaskManager.Core.FamilyAggregate;

namespace FamilyTaskManager.Core.Interfaces;

public interface IFamilyMemberReadRepository
{
  Task<FamilyMember?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
  Task<FamilyMember?> GetByIdWithUserAsync(Guid id, CancellationToken cancellationToken = default);
  Task<List<FamilyMember>> GetByFamilyIdAsync(Guid familyId, CancellationToken cancellationToken = default);
  Task<List<FamilyMember>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
