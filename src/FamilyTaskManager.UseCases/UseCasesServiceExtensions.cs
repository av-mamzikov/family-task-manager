using Microsoft.Extensions.DependencyInjection;

namespace FamilyTaskManager.UseCases;

public static class UseCasesServiceExtensions
{
  public static IServiceCollection AddUseCasesServices(this IServiceCollection services)
  {
    // UseCases layer contains command/query handlers
    // Mediator is registered in the Host layer where SourceGenerator can scan all assemblies

    return services;
  }
}
