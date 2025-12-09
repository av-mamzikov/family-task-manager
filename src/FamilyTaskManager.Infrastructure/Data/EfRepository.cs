using Ardalis.SharedKernel;
using Ardalis.Specification.EntityFrameworkCore;

namespace FamilyTaskManager.Infrastructure.Data;

// inherit from Ardalis.Specification type
public class EfAppRepository<T>(AppDbContext dbContext) :
  RepositoryBase<T>(dbContext), IAppReadRepository<T>, IAppRepository<T> where T : class, IAggregateRoot
{
  public async Task<T> GetOrCreateAndSaveAsync(
    ISpecification<T> findSpec,
    Func<T> createEntity,
    CancellationToken cancellationToken = default)
  {
    var existing = await FirstOrDefaultAsync(findSpec, cancellationToken);
    if (existing != null)
      return existing;

    try
    {
      var newEntity = createEntity();
      await AddAsync(newEntity, cancellationToken);
      await SaveChangesAsync(cancellationToken);
      return newEntity;
    }
    catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
    {
      var entity = await FirstOrDefaultAsync(findSpec, cancellationToken);
      if (entity != null)
        return entity;

      throw;
    }
  }

  private static bool IsUniqueConstraintViolation(DbUpdateException ex) =>
    ex.InnerException?.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true ||
    ex.InnerException?.Message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase) == true ||
    ex.InnerException?.Message.Contains("UNIQUE KEY", StringComparison.OrdinalIgnoreCase) == true;
}
