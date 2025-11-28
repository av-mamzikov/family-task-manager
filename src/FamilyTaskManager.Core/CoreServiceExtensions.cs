using FamilyTaskManager.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FamilyTaskManager.Core;

public static class CoreServiceExtensions
{
  public static IServiceCollection AddCoreServices(this IServiceCollection services)
  {
    // Register domain services
    services.AddScoped<ITaskInstanceFactory, TaskInstanceFactory>();
    services.AddScoped<IPetMoodCalculator, PetMoodCalculator>();

    return services;
  }
}
