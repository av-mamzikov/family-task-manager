using Mediator;

namespace FamilyTaskManager.Infrastructure.Behaviors;

/// <summary>
/// Pipeline behavior that logs all commands and queries
/// </summary>
public class MediatorLoggingBehavior<TMessage, TResponse>(
  ILogger<MediatorLoggingBehavior<TMessage, TResponse>> logger)
  : IPipelineBehavior<TMessage, TResponse>
  where TMessage : IMessage
{
  public async ValueTask<TResponse> Handle(
    TMessage message,
    MessageHandlerDelegate<TMessage, TResponse> next,
    CancellationToken cancellationToken)
  {
    var requestName = typeof(TMessage).Name;
    var requestNamespace = typeof(TMessage).Namespace;
    
    logger.LogInformation(
      "Executing {RequestType}: {RequestName}",
      requestNamespace?.Contains("Commands") == true ? "Command" : "Query",
      requestName);

    try
    {
      var response = await next(message, cancellationToken);
      
      logger.LogInformation(
        "Executed {RequestName} successfully",
        requestName);
      
      return response;
    }
    catch (Exception ex)
    {
      logger.LogError(
        ex,
        "Error executing {RequestName}",
        requestName);
      throw;
    }
  }
}
