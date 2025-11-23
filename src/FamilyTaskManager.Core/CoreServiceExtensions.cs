using Microsoft.Extensions.DependencyInjection;

namespace FamilyTaskManager.Core;

public static class CoreServiceExtensions
{
  public static IServiceCollection AddCoreServices(this IServiceCollection services)
  {
    // Core layer typically doesn't register services
    // It only contains domain entities, value objects, and interfaces
    // But we keep this method for consistency and future extensibility

    return services;
  }
}
