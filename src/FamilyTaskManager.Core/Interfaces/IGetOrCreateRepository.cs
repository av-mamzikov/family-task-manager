namespace FamilyTaskManager.Core.Interfaces;

public interface IAppRepository<T> : IRepository<T>
  where T : class, IAggregateRoot
{
  /// <summary>
  ///   Retrieves an entity by specification. If none is found, creates a new one and persists it to the database
  /// </summary>
  Task<T> GetOrCreateAndSaveAsync(
    ISpecification<T> findSpec,
    Func<T> createEntity,
    CancellationToken cancellationToken = default);
}
