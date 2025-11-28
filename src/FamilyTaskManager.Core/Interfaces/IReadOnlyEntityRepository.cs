namespace FamilyTaskManager.Core.Interfaces;

/// <summary>
///   Репозиторий для запроса обычных сущностей
/// </summary>
public interface IReadOnlyEntityRepository<T> where T : class
{
  Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
  Task<T?> FirstOrDefaultAsync(Func<T, bool> predicate, CancellationToken cancellationToken = default);
  Task<List<T>> ListAsync(CancellationToken cancellationToken = default);
  Task<List<T>> ListAsync(Func<T, bool> predicate, CancellationToken cancellationToken = default);
  Task<bool> AnyAsync(Func<T, bool> predicate, CancellationToken cancellationToken = default);

  // Specification-based methods
  Task<T?> FirstOrDefaultAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);
  Task<List<T>> ListAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);
  Task<int> CountAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);

  // Specification-based projection methods
  Task<TResult?> FirstOrDefaultProjectionAsync<TResult>(ISpecification<T, TResult> specification,
    CancellationToken cancellationToken = default);

  Task<List<TResult>> ListProjectionAsync<TResult>(ISpecification<T, TResult> specification,
    CancellationToken cancellationToken = default);
}
