using FamilyTaskManager.Core.FamilyAggregate;
using FamilyTaskManager.Core.Services;
using FamilyTaskManager.Core.SpotAggregate;
using FamilyTaskManager.Core.TaskAggregate;
using FamilyTaskManager.Infrastructure.Data;
using FamilyTaskManager.IntegrationTests.Data;
using FamilyTaskManager.TestInfrastructure;
using FamilyTaskManager.UseCases.Features.TasksManagement.Commands;
using FamilyTaskManager.UseCases.Features.TasksManagement.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace FamilyTaskManager.IntegrationTests.UseCases.Tasks;

public class ProcessScheduledTasksHandlerTests : IAsyncLifetime
{
  private AppDbContext _dbContext = null!;
  private PooledContainer _pooledContainer = null!;
  private TestRepositoryFactory _repositoryFactory = null!;

  public async Task InitializeAsync()
  {
    _pooledContainer = await PostgreSqlContainerPool<AppDbContext>.Instance.AcquireContainerAsync();
    _dbContext = _pooledContainer.GetDbContext();
    _repositoryFactory = new(_dbContext);
  }

  public async Task DisposeAsync()
  {
    await _dbContext.Database.CloseConnectionAsync();
    await _dbContext.DisposeAsync();
    await PostgreSqlContainerPool<AppDbContext>.Instance.ReleaseContainerAsync(_pooledContainer);
  }

  [RetryFact(2)]
  public async Task ProcessScheduledTasks_ShouldCreateSingleTaskInstance_AndNotDuplicateOnSecondRun()
  {
    // Arrange
    var family = new Family($"Test Family {Guid.NewGuid():N}", "UTC");
    var familyRepository = _repositoryFactory.GetRepository<Family>();
    await familyRepository.AddAsync(family);
    await _dbContext.SaveChangesAsync();

    var spot = new Spot(family.Id, SpotType.Cat, "Test Spot");
    var spotWriteRepository = _repositoryFactory.GetRepository<Spot>();
    await spotWriteRepository.AddAsync(spot);
    await _dbContext.SaveChangesAsync();

    var schedule = Schedule.CreateDaily(new(19, 0)).Value;
    var dueDuration = new DueDuration(TimeSpan.FromHours(3));
    var template = new TaskTemplate(
      family.Id,
      spot.Id,
      new("Поиграть с котом"),
      new(1),
      schedule,
      dueDuration,
      Guid.NewGuid());

    var templateRepository = _repositoryFactory.GetRepository<TaskTemplate>();
    await templateRepository.AddAsync(template);
    await _dbContext.SaveChangesAsync();

    var templateReadRepository = _repositoryFactory.GetReadRepository<TaskTemplate>();
    var taskInstanceRepository = _repositoryFactory.GetRepository<TaskInstance>();
    var spotRepository = _repositoryFactory.GetRepository<Spot>();
    var taskInstanceFactory = new TaskInstanceFactory();
    var logger = NullLogger<ProcessScheduledTasksHandler>.Instance;

    var assignedMemberSelector = Substitute.For<IAssignedMemberSelector>();
    assignedMemberSelector
      .SelectAssignedMemberAsync(Arg.Any<TaskTemplate>(), Arg.Any<Spot>(), Arg.Any<CancellationToken>())
      .Returns(ValueTask.FromResult<FamilyMember?>(null));

    var handler = new ProcessScheduledTasksHandler(
      templateReadRepository,
      taskInstanceRepository,
      spotRepository,
      taskInstanceFactory,
      logger,
      assignedMemberSelector);

    var today = DateTime.UtcNow.Date;
    var checkFrom = today.AddHours(18);
    var checkTo = today.AddHours(20);
    var command = new ProcessScheduledTaskCommand(checkFrom, checkTo);

    // Act: first run
    var result1 = await handler.Handle(command, CancellationToken.None);

    // Assert: first run created exactly one instance
    result1.IsSuccess.ShouldBeTrue();
    result1.Value.ShouldBe(1);

    var allInstancesAfterFirstRun = await taskInstanceRepository.ListAsync();
    allInstancesAfterFirstRun.Count.ShouldBe(1);
    var instance = allInstancesAfterFirstRun.Single();
    instance.TemplateId.ShouldBe(template.Id);
    instance.FamilyId.ShouldBe(family.Id);
    instance.SpotId.ShouldBe(spot.Id);

    var expectedDueAtLocal = today.AddHours(19) + dueDuration.Value;
    var expectedDueAtUtc = expectedDueAtLocal; // family timezone is UTC
    instance.DueAt.ShouldBe(expectedDueAtUtc);

    // Act: second run with the same window
    var result2 = await handler.Handle(command, CancellationToken.None);

    // Assert: second run should not create new instances
    result2.IsSuccess.ShouldBeTrue();
    result2.Value.ShouldBe(0);

    var allInstancesAfterSecondRun = await taskInstanceRepository.ListAsync();
    allInstancesAfterSecondRun.Count.ShouldBe(1);
    allInstancesAfterSecondRun.Single().Id.ShouldBe(instance.Id);
  }
}
