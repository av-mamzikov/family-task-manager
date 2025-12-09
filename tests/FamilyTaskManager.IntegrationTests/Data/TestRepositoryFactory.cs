using FamilyTaskManager.Core.Interfaces;
using FamilyTaskManager.Infrastructure.Data;

namespace FamilyTaskManager.IntegrationTests.Data;

public class TestRepositoryFactory(AppDbContext dbContext)
{
  private readonly AppDbContext _dbContext = dbContext;

  public IAppRepository<T> GetRepository<T>() where T : class, IAggregateRoot => new EfAppRepository<T>(_dbContext);

  public IAppReadRepository<T> GetReadRepository<T>() where T : class, IAggregateRoot =>
    new EfAppRepository<T>(_dbContext);
}
