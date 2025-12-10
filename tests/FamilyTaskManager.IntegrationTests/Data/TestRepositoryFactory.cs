using FamilyTaskManager.Core.Interfaces;
using FamilyTaskManager.Infrastructure.Data;

namespace FamilyTaskManager.IntegrationTests.Data;

public class TestRepositoryFactory(AppDbContext dbContext)
{
  public AppDbContext DbContext => dbContext;
  public IAppRepository<T> GetRepository<T>() where T : class, IAggregateRoot => new EfAppRepository<T>(dbContext);

  public IAppReadRepository<T> GetReadRepository<T>() where T : class, IAggregateRoot =>
    new EfAppRepository<T>(dbContext);

  public IReadOnlyEntityRepository<T> GetReadOnlyEntityRepository<T>() where T : class =>
    new EfReadOnlyEntityRepository<T>(dbContext);
}
