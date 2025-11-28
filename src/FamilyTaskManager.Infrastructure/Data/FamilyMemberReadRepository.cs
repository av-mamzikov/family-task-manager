using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.Interfaces;

namespace FamilyTaskManager.Infrastructure.Data;

public class FamilyMemberReadRepository(AppDbContext dbContext) : IFamilyMemberReadRepository
{
  public async Task<FamilyMember?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
    await dbContext.Set<FamilyMember>()
      .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

  public async Task<FamilyMember?> GetByIdWithUserAsync(Guid id, CancellationToken cancellationToken = default) =>
    await dbContext.Set<FamilyMember>()
      .Include(m => m.User)
      .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

  public async Task<List<FamilyMember>>
    GetByFamilyIdAsync(Guid familyId, CancellationToken cancellationToken = default) =>
    await dbContext.Set<FamilyMember>()
      .Where(m => m.FamilyId == familyId)
      .ToListAsync(cancellationToken);

  public async Task<List<FamilyMember>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
    await dbContext.Set<FamilyMember>()
      .Where(m => m.UserId == userId)
      .ToListAsync(cancellationToken);
}
