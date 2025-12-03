using Ardalis.Specification.EntityFrameworkCore;

namespace FamilyTaskManager.Infrastructure.Data;

public class EfReadOnlyEntityRepository<T>(AppDbContext dbContext) : IReadOnlyEntityRepository<T>
  where T : class
{
  private readonly ISpecificationEvaluator _specificationEvaluator = new SpecificationEvaluator();
  protected AppDbContext DbContext => dbContext;

  public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
    await dbContext.Set<T>().FindAsync(new object[] { id }, cancellationToken);

  public async Task<T?> FirstOrDefaultAsync(Func<T, bool> predicate, CancellationToken cancellationToken = default) =>
    await dbContext.Set<T>().FirstOrDefaultAsync(e => predicate(e), cancellationToken);

  public async Task<List<T>> ListAsync(CancellationToken cancellationToken = default) =>
    await dbContext.Set<T>().ToListAsync(cancellationToken);

  public async Task<List<T>> ListAsync(Func<T, bool> predicate, CancellationToken cancellationToken = default) =>
    await dbContext.Set<T>().Where(e => predicate(e)).ToListAsync(cancellationToken);

  public async Task<bool> AnyAsync(Func<T, bool> predicate, CancellationToken cancellationToken = default) =>
    await dbContext.Set<T>().AnyAsync(e => predicate(e), cancellationToken);

  // Specification-based methods
  public async Task<T?> FirstOrDefaultAsync(ISpecification<T> specification,
    CancellationToken cancellationToken = default) =>
    await ApplySpecification(specification).FirstOrDefaultAsync(cancellationToken);

  public async Task<List<T>>
    ListAsync(ISpecification<T> specification, CancellationToken cancellationToken = default) =>
    await ApplySpecification(specification).ToListAsync(cancellationToken);

  public async Task<int> CountAsync(ISpecification<T> specification, CancellationToken cancellationToken = default) =>
    await ApplySpecification(specification).CountAsync(cancellationToken);

  // Specification-based projection methods
  public async Task<TResult?> FirstOrDefaultProjectionAsync<TResult>(ISpecification<T, TResult> specification,
    CancellationToken cancellationToken = default) =>
    await ApplySpecification(specification).FirstOrDefaultAsync(cancellationToken);

  public async Task<List<TResult>> ListProjectionAsync<TResult>(ISpecification<T, TResult> specification,
    CancellationToken cancellationToken = default) =>
    await ApplySpecification(specification).ToListAsync(cancellationToken);

  private IQueryable<T> ApplySpecification(ISpecification<T> specification) =>
    _specificationEvaluator.GetQuery(dbContext.Set<T>(), specification);

  private IQueryable<TResult> ApplySpecification<TResult>(ISpecification<T, TResult> specification) =>
    _specificationEvaluator.GetQuery(dbContext.Set<T>(), specification);

  public AppDbContext GetDbContext() => dbContext;
}
