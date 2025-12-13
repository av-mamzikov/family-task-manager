using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.SpotAggregate;
using FamilyTaskManager.Core.UserAggregate;
using FamilyTaskManager.IntegrationTests.Data;
using FamilyTaskManager.UseCases.Features.SpotManagement.Commands;
using FamilyTaskManager.UseCases.Features.SpotManagement.Queries;

namespace FamilyTaskManager.IntegrationTests.UseCases.Spots;

public class SpotResponsiblesUseCasesTests : BaseRepositoryTestFixture
{
  private async Task<(Family family, User user, FamilyMember member, Spot spot)> CreateFamilyMemberAndSpotAsync()
  {
    var family = new Family($"Test Family {Guid.NewGuid():N}", "UTC");
    var user = new User(123456789L, "Test User");
    var member = family.AddMember(user, FamilyRole.Child);

    var familyRepository = RepositoryFactory.GetRepository<Family>();
    var userRepository = RepositoryFactory.GetRepository<User>();

    await userRepository.AddAsync(user);
    await familyRepository.AddAsync(family);
    await userRepository.SaveChangesAsync();
    await familyRepository.SaveChangesAsync();

    var spot = new Spot(family.Id, SpotType.Cat, "Test Spot");
    var spotRepository = RepositoryFactory.GetRepository<Spot>();
    await spotRepository.AddAsync(spot);
    await spotRepository.SaveChangesAsync();

    return (family, user, member, spot);
  }

  [RetryFact(2)]
  public async Task AssignSpotResponsible_ShouldPersistResponsibilityInDatabase()
  {
    var (_, _, member, spot) = await CreateFamilyMemberAndSpotAsync();

    var spotRepository = RepositoryFactory.GetRepository<Spot>();
    var familyMemberReadRepository = RepositoryFactory.GetReadOnlyEntityRepository<FamilyMember>();
    var handler = new AssignSpotResponsibleHandler(spotRepository, familyMemberReadRepository);

    var command = new AssignSpotResponsibleCommand(spot.Id, member.Id);

    var result = await handler.Handle(command, CancellationToken.None);

    result.IsSuccess.ShouldBeTrue();

    var reloadedSpot = await RepositoryFactory.DbContext.Spots
      .Include(s => s.ResponsibleMembers)
      .FirstAsync(s => s.Id == spot.Id);

    reloadedSpot.ResponsibleMembers.ShouldContain(m => m.Id == member.Id);
  }

  [RetryFact(2)]
  public async Task RemoveSpotResponsible_ShouldRemoveResponsibilityFromDatabase()
  {
    var (_, _, member, spot) = await CreateFamilyMemberAndSpotAsync();

    var spotRepository = RepositoryFactory.GetRepository<Spot>();
    var familyMemberReadRepository = RepositoryFactory.GetReadOnlyEntityRepository<FamilyMember>();
    var assignHandler = new AssignSpotResponsibleHandler(spotRepository, familyMemberReadRepository);
    var assignCommand = new AssignSpotResponsibleCommand(spot.Id, member.Id);
    var assignResult = await assignHandler.Handle(assignCommand, CancellationToken.None);
    assignResult.IsSuccess.ShouldBeTrue();

    RepositoryFactory.DbContext.ChangeTracker.Clear();

    var removeHandler = new RemoveSpotResponsibleHandler(spotRepository, familyMemberReadRepository);
    var removeCommand = new RemoveSpotResponsibleCommand(spot.Id, member.Id);

    var removeResult = await removeHandler.Handle(removeCommand, CancellationToken.None);

    removeResult.IsSuccess.ShouldBeTrue();

    var reloadedSpot = await RepositoryFactory.DbContext.Spots
      .Include(s => s.ResponsibleMembers)
      .FirstAsync(s => s.Id == spot.Id);

    reloadedSpot.ResponsibleMembers.ShouldNotContain(m => m.Id == member.Id);
  }

  [RetryFact(2)]
  public async Task GetMemberResponsibleSpots_ShouldReturnAssignedSpots()
  {
    var (_, _, member, spot) = await CreateFamilyMemberAndSpotAsync();

    var spotRepository = RepositoryFactory.GetRepository<Spot>();
    var familyMemberReadRepository = RepositoryFactory.GetReadOnlyEntityRepository<FamilyMember>();
    var assignHandler = new AssignSpotResponsibleHandler(spotRepository, familyMemberReadRepository);
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

  [RetryFact(2)]
  public async Task GetSpotResponsibleMembers_ShouldReturnOnlyActiveMembers()
  {
    var (family, user, member, spot) = await CreateFamilyMemberAndSpotAsync();

    // Create second member in the same family (will be deactivated later)
    var inactiveUser = new User(987654321L, "Inactive User");
    var inactiveMember = family.AddMember(inactiveUser, FamilyRole.Child);

    var familyRepository = RepositoryFactory.GetRepository<Family>();
    var userRepository = RepositoryFactory.GetRepository<User>();

    await userRepository.AddAsync(inactiveUser);
    await familyRepository.UpdateAsync(family);
    await userRepository.SaveChangesAsync();
    await familyRepository.SaveChangesAsync();

    // Assign both members as responsible
    var spotRepository = RepositoryFactory.GetRepository<Spot>();
    var familyMemberReadRepository = RepositoryFactory.GetReadOnlyEntityRepository<FamilyMember>();
    var assignHandler = new AssignSpotResponsibleHandler(spotRepository, familyMemberReadRepository);

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
    await familyRepository.SaveChangesAsync();

    // Query responsible members via use case
    var handler = new GetSpotResponsibleMembersHandler(familyMemberReadRepository);
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
