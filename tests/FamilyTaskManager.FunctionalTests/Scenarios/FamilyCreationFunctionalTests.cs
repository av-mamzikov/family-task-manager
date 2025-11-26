using FamilyTaskManager.Core.ActionHistoryAggregate;
using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Infrastructure.Data;
using FamilyTaskManager.UseCases.Families;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FamilyTaskManager.FunctionalTests.Scenarios;

/// <summary>
///   Functional tests for family creation scenarios (TS-001, TS-002, TS-003)
/// </summary>
public class FamilyCreationFunctionalTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
  private readonly CustomWebApplicationFactory<Program> _factory;

  public FamilyCreationFunctionalTests(CustomWebApplicationFactory<Program> factory)
  {
    _factory = factory;
  }

  [Fact]
  public async Task TS_002_CreateFirstFamily_ShouldSucceed()
  {
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var botClient = _factory.TelegramBotClient;

    // Clear bot interactions from previous tests
    botClient.Clear();

    var userId = Guid.NewGuid();
    var familyName = "Семья Ивановых";
    var timezone = "Europe/Moscow";

    // Act - Create family
    var createCommand = new CreateFamilyCommand(userId, familyName, timezone);
    var result = await mediator.Send(createCommand);

    // Assert - Family created successfully
    result.IsSuccess.ShouldBeTrue();
    result.Value.ShouldNotBe(Guid.Empty);

    // Note: You can verify bot interactions here if needed
    // Example: botClient.SentMessages.ShouldNotBeEmpty();

    // Verify family in database
    var family = await dbContext.Families
      .Include(f => f.Members)
      .FirstOrDefaultAsync(f => f.Id == result.Value);

    family.ShouldNotBeNull();
    family.Name.ShouldBe(familyName);
    family.Timezone.ShouldBe(timezone);

    // Verify user is admin
    var member = family.Members.FirstOrDefault(m => m.UserId == userId);
    member.ShouldNotBeNull();
    member.Role.ShouldBe(FamilyRole.Admin);
    member.Points.ShouldBe(0);

    // Verify action history
    var history = await dbContext.ActionHistory
      .Where(h => h.FamilyId == family.Id)
      .ToListAsync();

    history.ShouldNotBeEmpty();
    history.ShouldContain(h => h.ActionType == ActionType.FamilyCreated);
  }

  [Fact]
  public async Task TS_003_CreateFamilyWithShortName_ShouldFail()
  {
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

    var userId = Guid.NewGuid();
    var shortName = "Аб"; // Too short (< 3 characters)

    // Act
    var createCommand = new CreateFamilyCommand(userId, shortName, "UTC");
    var result = await mediator.Send(createCommand);

    // Assert - Should fail validation
    result.IsSuccess.ShouldBeFalse();
    result.ValidationErrors.ShouldNotBeEmpty();
  }

  [Fact]
  public async Task CreateMultipleFamilies_ShouldAllowUserToHaveMultipleFamilies()
  {
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    var userId = Guid.NewGuid();

    // Act - Create two families
    var family1Command = new CreateFamilyCommand(userId, "Семья 1", "UTC");
    var family1Result = await mediator.Send(family1Command);

    var family2Command = new CreateFamilyCommand(userId, "Семья 2", "Europe/Moscow");
    var family2Result = await mediator.Send(family2Command);

    // Assert
    family1Result.IsSuccess.ShouldBeTrue();
    family2Result.IsSuccess.ShouldBeTrue();

    // Verify user is member of both families
    var userFamilies = await dbContext.FamilyMembers
      .Where(m => m.UserId == userId && m.IsActive)
      .ToListAsync();

    userFamilies.Count.ShouldBe(2);
    userFamilies.ShouldAllBe(m => m.Role == FamilyRole.Admin);
  }

  [Fact]
  public async Task GetUserFamilies_ShouldReturnAllActiveFamilies()
  {
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

    var userId = Guid.NewGuid();

    // Create families
    await mediator.Send(new CreateFamilyCommand(userId, "Семья А", "UTC"));
    await mediator.Send(new CreateFamilyCommand(userId, "Семья Б", "UTC"));

    // Act
    var query = new GetUserFamiliesQuery(userId);
    var result = await mediator.Send(query);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    result.Value.Count.ShouldBe(2);
    result.Value.ShouldAllBe(f => f.UserRole == FamilyRole.Admin);
  }
}
