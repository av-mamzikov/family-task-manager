using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.SpotAggregate;
using FamilyTaskManager.Core.TaskAggregate;
using FamilyTaskManager.Core.UserAggregate;
using FamilyTaskManager.IntegrationTests.Data;
using FamilyTaskManager.UseCases.Features.TaskTemplatesManagement.Commands;
using FamilyTaskManager.UseCases.Features.TaskTemplatesManagement.Queries;

namespace FamilyTaskManager.IntegrationTests.UseCases.TaskTemplates;

public class TaskTemplateResponsiblesUseCasesTests : BaseRepositoryTestFixture
{
  private async Task<(Family family, User user, FamilyMember member, Spot spot, TaskTemplate taskTemplate)> CreateFamilyMemberSpotAndTemplateAsync()
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

    var schedule = Schedule.CreateDaily(new TimeOnly(9, 0)).Value;
    var dueDuration = new DueDuration(TimeSpan.FromHours(2));
    var taskTemplate = new TaskTemplate(
      family.Id,
      spot.Id,
      new TaskTitle("Test Task Template"),
      new TaskPoints(3),
      schedule,
      dueDuration,
      member.Id);

    var taskTemplateRepository = RepositoryFactory.GetRepository<TaskTemplate>();
    await taskTemplateRepository.AddAsync(taskTemplate);
    await taskTemplateRepository.SaveChangesAsync();

    return (family, user, member, spot, taskTemplate);
  }

  [RetryFact(2)]
  public async Task AssignTaskTemplateResponsible_ShouldPersistResponsibilityInDatabase()
  {
    var (_, _, member, _, taskTemplate) = await CreateFamilyMemberSpotAndTemplateAsync();

    var taskTemplateRepository = RepositoryFactory.GetRepository<TaskTemplate>();
    var familyMemberReadRepository = RepositoryFactory.GetReadOnlyEntityRepository<FamilyMember>();
    var handler = new AssignTaskTemplateResponsibleHandler(taskTemplateRepository, familyMemberReadRepository);

    var command = new AssignTaskTemplateResponsibleCommand(taskTemplate.Id, member.Id);

    var result = await handler.Handle(command, CancellationToken.None);

    result.IsSuccess.ShouldBeTrue();

    var reloadedTemplate = await RepositoryFactory.DbContext.TaskTemplates
      .Include(t => t.ResponsibleMembers)
      .FirstAsync(t => t.Id == taskTemplate.Id);

    reloadedTemplate.ResponsibleMembers.ShouldContain(m => m.Id == member.Id);
  }

  [RetryFact(2)]
  public async Task RemoveTaskTemplateResponsible_ShouldRemoveResponsibilityFromDatabase()
  {
    var (_, _, member, _, taskTemplate) = await CreateFamilyMemberSpotAndTemplateAsync();

    var taskTemplateRepository = RepositoryFactory.GetRepository<TaskTemplate>();
    var familyMemberReadRepository = RepositoryFactory.GetReadOnlyEntityRepository<FamilyMember>();
    var assignHandler = new AssignTaskTemplateResponsibleHandler(taskTemplateRepository, familyMemberReadRepository);
    var assignCommand = new AssignTaskTemplateResponsibleCommand(taskTemplate.Id, member.Id);
    var assignResult = await assignHandler.Handle(assignCommand, CancellationToken.None);
    assignResult.IsSuccess.ShouldBeTrue();

    RepositoryFactory.DbContext.ChangeTracker.Clear();

    var removeHandler = new RemoveTaskTemplateResponsibleHandler(taskTemplateRepository, familyMemberReadRepository);
    var removeCommand = new RemoveTaskTemplateResponsibleCommand(taskTemplate.Id, member.Id);

    var removeResult = await removeHandler.Handle(removeCommand, CancellationToken.None);

    removeResult.IsSuccess.ShouldBeTrue();

    var reloadedTemplate = await RepositoryFactory.DbContext.TaskTemplates
      .Include(t => t.ResponsibleMembers)
      .FirstAsync(t => t.Id == taskTemplate.Id);

    reloadedTemplate.ResponsibleMembers.ShouldNotContain(m => m.Id == member.Id);
  }

  [RetryFact(2)]
  public async Task ClearTaskTemplateResponsible_ShouldRemoveAllResponsibilities()
  {
    var (family, _, member1, _, taskTemplate) = await CreateFamilyMemberSpotAndTemplateAsync();

    // Create second member in the same family
    var user2 = new User(987654321L, "Test User 2");
    var member2 = family.AddMember(user2, FamilyRole.Child);
    var familyRepository = RepositoryFactory.GetRepository<Family>();
    var userRepository = RepositoryFactory.GetRepository<User>();
    await userRepository.AddAsync(user2);
    await familyRepository.UpdateAsync(family);
    await userRepository.SaveChangesAsync();
    await familyRepository.SaveChangesAsync();

    var taskTemplateRepository = RepositoryFactory.GetRepository<TaskTemplate>();
    var familyMemberReadRepository = RepositoryFactory.GetReadOnlyEntityRepository<FamilyMember>();
    
    // Assign both members as responsible
    var assignHandler = new AssignTaskTemplateResponsibleHandler(taskTemplateRepository, familyMemberReadRepository);
    
    var assignResult1 = await assignHandler.Handle(
      new AssignTaskTemplateResponsibleCommand(taskTemplate.Id, member1.Id),
      CancellationToken.None);
    assignResult1.IsSuccess.ShouldBeTrue();

    var assignResult2 = await assignHandler.Handle(
      new AssignTaskTemplateResponsibleCommand(taskTemplate.Id, member2.Id),
      CancellationToken.None);
    assignResult2.IsSuccess.ShouldBeTrue();

    RepositoryFactory.DbContext.ChangeTracker.Clear();

    // Clear all responsible members
    var clearHandler = new ClearTaskTemplateResponsibleHandler(taskTemplateRepository);
    var clearCommand = new ClearTaskTemplateResponsibleCommand(taskTemplate.Id);

    var clearResult = await clearHandler.Handle(clearCommand, CancellationToken.None);

    clearResult.IsSuccess.ShouldBeTrue();

    var reloadedTemplate = await RepositoryFactory.DbContext.TaskTemplates
      .Include(t => t.ResponsibleMembers)
      .FirstAsync(t => t.Id == taskTemplate.Id);

    reloadedTemplate.ResponsibleMembers.ShouldBeEmpty();
  }

  [RetryFact(2)]
  public async Task GetTaskTemplateResponsibleMembers_ShouldReturnOnlyActiveMembers()
  {
    var (family, user1, member1, _, taskTemplate) = await CreateFamilyMemberSpotAndTemplateAsync();

    // Create second member in the same family (will be deactivated later)
    var user2 = new User(987654321L, "Inactive User");
    var member2 = family.AddMember(user2, FamilyRole.Child);

    var familyRepository = RepositoryFactory.GetRepository<Family>();
    var userRepository = RepositoryFactory.GetRepository<User>();

    await userRepository.AddAsync(user2);
    await familyRepository.UpdateAsync(family);
    await userRepository.SaveChangesAsync();
    await familyRepository.SaveChangesAsync();

    // Assign both members as responsible
    var taskTemplateRepository = RepositoryFactory.GetRepository<TaskTemplate>();
    var familyMemberReadRepository = RepositoryFactory.GetReadOnlyEntityRepository<FamilyMember>();
    var assignHandler = new AssignTaskTemplateResponsibleHandler(taskTemplateRepository, familyMemberReadRepository);

    var assignActive = await assignHandler.Handle(
      new AssignTaskTemplateResponsibleCommand(taskTemplate.Id, member1.Id),
      CancellationToken.None);
    assignActive.IsSuccess.ShouldBeTrue();

    var assignInactive = await assignHandler.Handle(
      new AssignTaskTemplateResponsibleCommand(taskTemplate.Id, member2.Id),
      CancellationToken.None);
    assignInactive.IsSuccess.ShouldBeTrue();

    // Deactivate second member and persist change
    member2.Deactivate();
    await familyRepository.UpdateAsync(family);
    await familyRepository.SaveChangesAsync();

    // Query responsible members via use case
    var handler = new GetTaskTemplateResponsibleMembersHandler(familyMemberReadRepository);
    var query = new GetTaskTemplateResponsibleMembersQuery(taskTemplate.Id);

    var result = await handler.Handle(query, CancellationToken.None);

    result.IsSuccess.ShouldBeTrue();
    result.Value.ShouldNotBeNull();

    // Only active member should be returned
    result.Value.Count.ShouldBe(1);
    var dto = result.Value.Single();
    dto.Id.ShouldBe(member1.Id);
    dto.UserId.ShouldBe(user1.Id);
    dto.FamilyId.ShouldBe(family.Id);
  }
}