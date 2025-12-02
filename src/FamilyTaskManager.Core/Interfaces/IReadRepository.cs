namespace FamilyTaskManager.Core.Interfaces;

public interface IReadRepository<T> : Ardalis.SharedKernel.IReadRepository<T> where T : class, IAggregateRoot
{
}
