using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.SpotAggregate;
using FamilyTaskManager.Core.UserAggregate;
using FamilyTaskManager.Infrastructure.Data;
using FamilyTaskManager.IntegrationTests.Data;
using FamilyTaskManager.TestInfrastructure;
using FamilyTaskManager.UseCases.Spots;

namespace FamilyTaskManager.IntegrationTests.UseCases.Spots;

public class SpotResponsiblesUseCasesTests : IAsyncLifetime
{
  private AppDbContext _dbContext = null!;
  private EfReadOnlyEntityRepository<FamilyMember> _familyMemberReadRepository = null!;
  private PooledContainer _pooledContainer = null!;
  private TestRepositoryFactory _repositoryFactory = null!;

  public async Task InitializeAsync()
  {
    _pooledContainer = await PostgreSqlContainerPool<AppDbContext>.Instance.AcquireContainerAsync();

    var options = new DbContextOptionsBuilder<AppDbContext>()
      .UseNpgsql(_pooledContainer.GetConnectionString())
      .EnableSensitiveDataLogging()
      .Options;

    _dbContext = new(options);
    _repositoryFactory = new(_dbContext);
    _familyMemberReadRepository = new(_dbContext);
  }

  public async Task DisposeAsync()
  {
    await _dbContext.Database.CloseConnectionAsync();
    await _dbContext.DisposeAsync();
    await PostgreSqlContainerPool<AppDbContext>.Instance.ReleaseContainerAsync(_pooledContainer);
  }

  private async Task<(Family family, User user, FamilyMember member, Spot spot)> CreateFamilyMemberAndSpotAsync()
  {
    var family = new Family($"Test Family {Guid.NewGuid():N}", "UTC");
    var user = new User(123456789L, "Test User");
    var member = family.AddMember(user, FamilyRole.Child);

    var familyRepository = _repositoryFactory.GetRepository<Family>();
    var userRepository = _repositoryFactory.GetRepository<User>();

    await userRepository.AddAsync(user);
    await familyRepository.AddAsync(family);
    await _dbContext.SaveChangesAsync();

    var spot = new Spot(family.Id, SpotType.Cat, "Test Spot");
    var spotRepository = _repositoryFactory.GetRepository<Spot>();
    await spotRepository.AddAsync(spot);
    await _dbContext.SaveChangesAsync();

    return (family, user, member, spot);
  }

  [Fact]
  public async Task AssignSpotResponsible_ShouldPersistResponsibilityInDatabase()
  {
    var (_, _, member, spot) = await CreateFamilyMemberAndSpotAsync();

    var spotRepository = _repositoryFactory.GetRepository<Spot>();
    var handler = new AssignSpotResponsibleHandler(spotRepository, _familyMemberReadRepository);

    var command = new AssignSpotResponsibleCommand(spot.Id, member.Id);

    var result = await handler.Handle(command, CancellationToken.None);

    result.IsSuccess.ShouldBeTrue();

    var reloadedSpot = await _dbContext.Spots
      .Include(s => s.ResponsibleMembers)
      .FirstAsync(s => s.Id == spot.Id);

    reloadedSpot.ResponsibleMembers.ShouldContain(m => m.Id == member.Id);
  }

  [Fact]
  public async Task RemoveSpotResponsible_ShouldRemoveResponsibilityFromDatabase()
  {
    var (_, _, member, spot) = await CreateFamilyMemberAndSpotAsync();

    var spotRepository = _repositoryFactory.GetRepository<Spot>();
    var assignHandler = new AssignSpotResponsibleHandler(spotRepository, _familyMemberReadRepository);
    var assignCommand = new AssignSpotResponsibleCommand(spot.Id, member.Id);
    var assignResult = await assignHandler.Handle(assignCommand, CancellationToken.None);
    assignResult.IsSuccess.ShouldBeTrue();

    _dbContext.ChangeTracker.Clear();

    var removeHandler = new RemoveSpotResponsibleHandler(spotRepository, _familyMemberReadRepository);
    var removeCommand = new RemoveSpotResponsibleCommand(spot.Id, member.Id);

    var removeResult = await removeHandler.Handle(removeCommand, CancellationToken.None);

    removeResult.IsSuccess.ShouldBeTrue();

    var reloadedSpot = await _dbContext.Spots
      .Include(s => s.ResponsibleMembers)
      .FirstAsync(s => s.Id == spot.Id);

    reloadedSpot.ResponsibleMembers.ShouldNotContain(m => m.Id == member.Id);
  }

  [Fact]
  public async Task GetMemberResponsibleSpots_ShouldReturnAssignedSpots()
  {
    var (_, _, member, spot) = await CreateFamilyMemberAndSpotAsync();

    var spotRepository = _repositoryFactory.GetRepository<Spot>();
    var assignHandler = new AssignSpotResponsibleHandler(spotRepository, _familyMemberReadRepository);
    var assignCommand = new AssignSpotResponsibleCommand(spot.Id, member.Id);
    var assignResult = await assignHandler.Handle(assignCommand, CancellationToken.None);
    assignResult.IsSuccess.ShouldBeTrue();

    var queryHandler = new GetMemberResponsibleSpotsHandler(spotRepository);
    var query = new GetMemberResponsibleSpotsQuery(member.Id);

    var result = await queryHandler.Handle(query, CancellationToken.None);

    result.IsSuccess.ShouldBeTrue();
    result.Value.ShouldNotBeNull();
    result.Value.Count.ShouldBe(1);

    var dto = result.Value.Single();
    dto.Id.ShouldBe(spot.Id);
    dto.FamilyId.ShouldBe(spot.FamilyId);
  }

  [Fact]
  public async Task GetSpotResponsibleMembers_ShouldReturnOnlyActiveMembers()
  {
    var (family, user, member, spot) = await CreateFamilyMemberAndSpotAsync();

    // Create second member in the same family (will be deactivated later)
    var inactiveUser = new User(987654321L, "Inactive User");
    var inactiveMember = family.AddMember(inactiveUser, FamilyRole.Child);

    var familyRepository = _repositoryFactory.GetRepository<Family>();
    var userRepository = _repositoryFactory.GetRepository<User>();

    await userRepository.AddAsync(inactiveUser);
    await familyRepository.UpdateAsync(family);
    await _dbContext.SaveChangesAsync();

    // Assign both members as responsible
    var spotRepository = _repositoryFactory.GetRepository<Spot>();
    var assignHandler = new AssignSpotResponsibleHandler(spotRepository, _familyMemberReadRepository);

    var assignActive = await assignHandler.Handle(
      new(spot.Id, member.Id),
      CancellationToken.None);
    assignActive.IsSuccess.ShouldBeTrue();

    var assignInactive = await assignHandler.Handle(
      new(spot.Id, inactiveMember.Id),
      CancellationToken.None);
    assignInactive.IsSuccess.ShouldBeTrue();

    // Deactivate second member and persist change
    inactiveMember.Deactivate();
    await familyRepository.UpdateAsync(family);
    await _dbContext.SaveChangesAsync();

    // Query responsible members via use case
    var handler = new GetSpotResponsibleMembersHandler(_familyMemberReadRepository);
    var query = new GetSpotResponsibleMembersQuery(spot.Id);

    var result = await handler.Handle(query, CancellationToken.None);

    result.IsSuccess.ShouldBeTrue();
    result.Value.ShouldNotBeNull();

    // Only active member should be returned
    result.Value.Count.ShouldBe(1);
    var dto = result.Value.Single();
    dto.Id.ShouldBe(member.Id);
    dto.UserId.ShouldBe(user.Id);
    dto.FamilyId.ShouldBe(family.Id);
  }
}
