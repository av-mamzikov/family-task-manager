namespace FamilyTaskManager.Core.Interfaces;

public interface IAppReadRepository<T> : IReadRepository<T> where T : class, IAggregateRoot
{
}
