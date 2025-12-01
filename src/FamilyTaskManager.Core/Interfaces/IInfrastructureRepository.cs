namespace FamilyTaskManager.Core.Interfaces;

/// <summary>
///   Repository for infrastructure entities (non-domain aggregates).
///   Used for entities like Outbox, audit logs, etc.
/// </summary>
public interface IInfrastructureRepository<T> where T : class
{
  Task AddAsync(T entity, CancellationToken cancellationToken = default);
  Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
  Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
  Task DeleteAsync(T entity, CancellationToken cancellationToken = default);
  Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
  Task<List<T>> ListAsync(CancellationToken cancellationToken = default);
  Task<List<T>> ListAsync(Func<T, bool> predicate, CancellationToken cancellationToken = default);
  Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
