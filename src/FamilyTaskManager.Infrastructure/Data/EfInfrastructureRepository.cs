using FamilyTaskManager.Core.Interfaces;

namespace FamilyTaskManager.Infrastructure.Data;

/// <summary>
///   EF implementation of infrastructure repository for non-domain entities.
/// </summary>
public class EfInfrastructureRepository<T>(AppDbContext dbContext) : IInfrastructureRepository<T>
  where T : class
{
  public async Task AddAsync(T entity, CancellationToken cancellationToken = default) =>
    await dbContext.Set<T>().AddAsync(entity, cancellationToken);

  public async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default) =>
    await dbContext.Set<T>().AddRangeAsync(entities, cancellationToken);

  public Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
  {
    dbContext.Set<T>().Update(entity);
    return Task.CompletedTask;
  }

  public Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
  {
    dbContext.Set<T>().Remove(entity);
    return Task.CompletedTask;
  }

  public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
    await dbContext.Set<T>().FindAsync(new object[] { id }, cancellationToken);

  public async Task<List<T>> ListAsync(CancellationToken cancellationToken = default) =>
    await dbContext.Set<T>().ToListAsync(cancellationToken);

  public async Task<List<T>> ListAsync(Func<T, bool> predicate, CancellationToken cancellationToken = default) =>
    await dbContext.Set<T>().Where(e => predicate(e)).ToListAsync(cancellationToken);

  public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
    await dbContext.SaveChangesAsync(cancellationToken);
}
