namespace FamilyTaskManager.UnitTests;

public class NoOpMediator : IMediator
{
  public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request,
    CancellationToken cancellationToken = default) =>
    EmptyAsyncEnumerable<TResponse>();

  public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamCommand<TResponse> command,
    CancellationToken cancellationToken = default) =>
    EmptyAsyncEnumerable<TResponse>();

  public IAsyncEnumerable<object?> CreateStream(object message, CancellationToken cancellationToken = default) =>
    EmptyAsyncEnumerable<object?>();

  public ValueTask Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
    where TNotification : INotification =>
    ValueTask.CompletedTask;

  public ValueTask Publish(object notification, CancellationToken cancellationToken = default) =>
    ValueTask.CompletedTask;

  public ValueTask<TResponse> Send<TResponse>(IRequest<TResponse> request,
    CancellationToken cancellationToken = default) =>
    ValueTask.FromResult(default(TResponse)!);

  public ValueTask<TResponse> Send<TResponse>(ICommand<TResponse> command,
    CancellationToken cancellationToken = default) =>
    ValueTask.FromResult(default(TResponse)!);

  public ValueTask<TResponse> Send<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default) =>
    ValueTask.FromResult(default(TResponse)!);

  public ValueTask<object?> Send(object message, CancellationToken cancellationToken = default) =>
    ValueTask.FromResult<object?>(null);

  IAsyncEnumerable<TResponse> ISender.CreateStream<TResponse>(IStreamQuery<TResponse> query,
    CancellationToken cancellationToken) =>
    EmptyAsyncEnumerable<TResponse>();

  public async Task<IAsyncEnumerable<TResponse>> CreateStream<TResponse>(IStreamQuery<TResponse> query,
    CancellationToken cancellationToken = default)
  {
    await Task.Delay(1);
    return EmptyAsyncEnumerable<TResponse>();
  }

  private static async IAsyncEnumerable<T> EmptyAsyncEnumerable<T>()
  {
    await Task.CompletedTask;
    yield break;
  }
}
