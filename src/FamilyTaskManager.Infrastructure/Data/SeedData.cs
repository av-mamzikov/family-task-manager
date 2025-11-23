namespace FamilyTaskManager.Infrastructure.Data;

public static class SeedData
{
  public static async Task InitializeAsync(AppDbContext dbContext)
  {
    // Seed data will be added here as needed
    await Task.CompletedTask;
  }

  public static async Task PopulateTestDataAsync(AppDbContext dbContext)
  {
    // Test data population will be added here
    await Task.CompletedTask;
  }
}
